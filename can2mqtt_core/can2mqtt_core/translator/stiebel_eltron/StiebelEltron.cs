// Thanks to:
// http://juerg5524.ch/list_data.php
// https://wiki.c3re.de/index.php/Projekt_23_Smarthome_/_Zugriff_Heizung

namespace can2mqtt.Translator.StiebelEltron
{

    /// <summary>
    /// This Translator class translates the CAN Bus data to values
    /// Validated with:
    /// - Stiebel Eltron LWZ 504
    /// </summary>
    public class StiebelEltron : ITranslator
    {
        private readonly ILogger Logger;

        private readonly FallbackValueConverter fallbackValueConverter = new();
        // initialize elster index once
        private static readonly ElsterIndex ElsterIndex = new();
        private static Lazy<IEnumerable<string>> MqttTopicsToPollList = new(() => ElsterIndex.ElsterIndexTable.Where(x => !x.IgnorePolling).Select(x => x.MqttTopic).ToList());

        private readonly IDictionary<int, string> partialCombinedValues = new Dictionary<int, string>();

        public StiebelEltron(ILoggerFactory loggerFactory) {
            Logger = loggerFactory.CreateLogger("StiebelEltronTranslator");
        }

        public IEnumerable<string> MqttTopicsToPoll {
            get {
                return MqttTopicsToPollList.Value;
            }
        }

        public CanFrame Translate(CanFrame rawData, bool noUnit, string language, bool convertUnknown)
        {
            //Check if format is correct
            if (string.IsNullOrEmpty(rawData.PayloadFull) || rawData.PayloadFull.Length != 14)
            {
                Logger.LogInformation("Data is not lenght of 14: {0}", rawData.PayloadFull);
                return rawData;
            }

            //- ModulType based in ComfortSoft-Protocol, 2. Byte (see robots, haustechnikdialog community):
            //0 - write
            //1 - read
            //2 - response
            //3 - ack
            //4 - write ack
            //5 - write respond
            //6 - system
            //7 - system respond
            //20/21 (hex.) - write/read large telegram
    
            var payloadIndex = Convert.ToInt32(rawData.ValueIndex, 16);
            var payloadData = rawData.Value;

            //Get IndexData
            var indexData = ElsterIndex.ElsterIndexTable.FirstOrDefault(x => x.Index == payloadIndex && x.Sender.ToString("X2") == rawData.PayloadSenderCanId );
            indexData ??= ElsterIndex.ElsterIndexTable.FirstOrDefault(x => x.Index == payloadIndex);
            indexData ??= ElsterIndex.ElsterIndexTable.FirstOrDefault(x => x.CombineIndex == payloadIndex);

            //Index not available
            if (indexData == null) {
                if (convertUnknown) {
                    Logger.LogInformation($"Fallback convertion:{Environment.NewLine}{fallbackValueConverter.ConvertValue(payloadData)}");
                }
                return rawData;
            }

            rawData.MqttTopicExtention = indexData.MqttTopic;

            if (indexData.CombineIndex.HasValue) { // combined index value handling
                var convertedValue = indexData.CombinedConverter.ConvertValue(payloadIndex, payloadData);
                if (!partialCombinedValues.TryGetValue(indexData.Index, out var payloadFragment)) {
                    partialCombinedValues.Add(indexData.Index, convertedValue);
                    rawData.IsComplete = false;
                    return rawData;
                }

                rawData.MqttValue = $"{indexData.CombinedConverter.CombineValues(payloadFragment, convertedValue)}{(!noUnit ? indexData.Unit : string.Empty)}";
                partialCombinedValues.Remove(indexData.Index);
                rawData.IsComplete = true;
                return rawData;
            }

            rawData.IsComplete = true;
            if (indexData.Converter == null) //custom converter
            {
                try
                {
                    rawData.MqttValue = indexData.ValueList[language][payloadData];
                }
                catch (Exception)
                {
                    Logger.LogInformation("No value for payloaddata {0} and language {1} found. Trying english values...", payloadData, language);
                    try
                    {
                        rawData.MqttValue = indexData.ValueList["EN"][payloadData];
                    }
                    catch (Exception)
                    {
                        Logger.LogInformation("No value for payloaddata {0} and language EN found. This is an unknown value.", payloadData);
                        rawData.MqttValue = "Unknown Data";
                    }
                }
            }
            else
                rawData.MqttValue = $"{indexData.Converter.ConvertValue(payloadData)}{(!noUnit ? indexData.Unit : string.Empty)}";

            return rawData;
        }

        /// <summary>
        /// Converts MQTT data to a CAN frame
        /// </summary>
        /// <param name="topic">The MQTT topic</param>
        /// <param name="value">The value of the corresponding MQTT topic</param>
        /// <param name="senderId">The sender ID in the CAN bus</param>
        /// <param name="noUnit">Include/Exclude Units at conversion</param>
        /// <param name="canOperation">The operating of the CAN bus (write = 0, read = 1)</param>
        /// <returns>List of can frames to send.</returns>
        public IEnumerable<string> TranslateBack(string topic, string value, string senderId, bool noUnit, string canOperation = "0")
        {
            if (topic.EndsWith("/set")) //remove the /set from topic
                topic = topic.Substring(0, topic.Length - 4);
            else if (topic.EndsWith("/read")) //remove the /read from topic
                topic = topic.Substring(0, topic.Length - 5);

            //remove the first topic because it is the custom one from the config file and not in the ElsterTable config file            
            var firstTopicStartIndex = topic.IndexOf('/');
            if (firstTopicStartIndex != -1)
            {
                topic = topic.Substring(firstTopicStartIndex);
            }

            //check the sender length. Fail in case of unexpected length
            if (senderId.Length != 3)
            {
                Logger.LogError("The senderID has not the length of 3.");
                return [];
            }

            // Get the Elster Index by the topic
            var elsterItem = ElsterIndex.ElsterIndexTable.FirstOrDefault(x => x.MqttTopic == topic);

            //Index not available
            if (elsterItem == null)
            {
                Logger.LogError("Cannot find a Elster item that has the MQTT topic {0}", topic);
                return [];
            }

            if (elsterItem.CombineIndex.HasValue && canOperation == "0")
            {
                Logger.LogError("Cannot write to a combined index. Please use the single indexes.");
                return [];
            }

            //Remove the Unit from the value
            if (!noUnit && value != null && value.EndsWith(elsterItem.Unit))
                value = value.Substring(0, value.Length - elsterItem.Unit.Length);

            // Result data must look like 3000FA056C0002
            // It will start with 3000FA (Receiver is 180, it is a write and Elster Index is used)
            // Followed by 4 characters of the index
            // finialized by 4 characters of the new value to set.

            string[] receiverId = new string[2];
            if (elsterItem.Sender >= 0x680)
            {
                receiverId[0] = "D";
                receiverId[1] = (elsterItem.Sender - 0x680).ToString("X2");
            }
            else if (elsterItem.Sender >= 0x600)
            {
                receiverId[0] = "C";
                receiverId[1] = (elsterItem.Sender - 0x600).ToString("X2");
            }
            else if (elsterItem.Sender >= 0x500)
            {
                receiverId[0] = "A";
                receiverId[1] = (elsterItem.Sender - 0x500).ToString("X2");
            }
            else if (elsterItem.Sender >= 0x480)
            {
                receiverId[0] = "9";
                receiverId[1] = (elsterItem.Sender - 0x480).ToString("X2");
            }
            else if (elsterItem.Sender >= 0x300)
            {
                receiverId[0] = "6";
                receiverId[1] = (elsterItem.Sender - 0x300).ToString("X2");
            }
            else if (elsterItem.Sender >= 0x180)
            {
                receiverId[0] = "3";
                receiverId[1] = (elsterItem.Sender - 0x180).ToString("X2");
            }
            else
            {
                receiverId[0] = "0"; // None
                receiverId[1] = elsterItem.Sender.ToString("X2");
            }

            // Convert the payload value to hex value
            var hexPayload = "0000";
            if (value != null)
            {
                hexPayload = elsterItem.Converter.ConvertValueBack(value);
            }

            // The first 3 characters are the sender Id
            // followed by #
            // followed by Receiver Index ID  (i.e. 3 for 180 or higher)
            // followed by canOperation read or write from/to the CAN bus (0 = write, 1 = read)
            // followed by 00 which is the offset of the receiver ID.
            // followed by FA to indicate the usage of the Elster Indexes
            // followed by the elster Index ID
            // followed by the value
            List<string> result = [CreateCanFrame(senderId, canOperation, elsterItem.Index, receiverId, hexPayload)];

            if (elsterItem.CombineIndex.HasValue)
            {
                result.Add(CreateCanFrame(senderId, canOperation, elsterItem.CombineIndex.Value, receiverId, hexPayload));
            }

            return result;
        }

        private string CreateCanFrame(string senderId, string canOperation, int index, string[] receiverId, string hexPayload)
        {
            var canFrameString = string.Format("{0}#{1}{2}{3}FA{4}{5}", senderId, receiverId[0], canOperation, receiverId[1], index.ToString("X4"), hexPayload);

            Logger.LogInformation("CAN Frame is: {0}", canFrameString);
            //Verify Format of the translated back data
            if (canFrameString.Length != 18)
            {
                Logger.LogError("Data is not lenght of 14: {0}", canFrameString);
                return string.Empty;
            }

            return canFrameString;
        }
    }
}
