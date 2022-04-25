// Thanks to:
// http://juerg5524.ch/list_data.php
// https://wiki.c3re.de/index.php/Projekt_23_Smarthome_/_Zugriff_Heizung


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace can2mqtt.Translator.StiebelEltron
{
    /// <summary>
    /// This Translator class translates the CAN Bus data to values
    /// Validated with:
    /// - Stiebel Eltron LWZ 504
    /// </summary>
    public class StiebelEltron : ITranslator
    {
        public CanFrame Translate(CanFrame rawData, bool noUnit)
        {
            //Check if format is correct
            if (string.IsNullOrEmpty(rawData.PayloadFull) || rawData.PayloadFull.Length != 14)
            {
                Console.WriteLine("Data is not lenght of 14: {0}", rawData.PayloadFull);
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

            var payloadIndex = Convert.ToInt32(rawData.PayloadFull.Substring(6, 4), 16);
            var payloadData = rawData.PayloadFull.Substring(10);

            //Get IndexData
            var elsterTable = new ElsterIndex();
            var indexData = elsterTable.ElsterIndexTable.FirstOrDefault(x => x.Index == payloadIndex && x.Sender.ToString("X2") == rawData.PayloadSenderCanId );
            if (indexData == null)
                indexData = elsterTable.ElsterIndexTable.FirstOrDefault(x => x.Index == payloadIndex);
            

            //Index not available
            if (indexData == null)
                return rawData;

            rawData.MqttTopicExtention = indexData.MqttTopic;

            if (indexData.Converter == null) //custom converter
            {
                //0000=Aus,0001=Schließer-Aus,0002=Öffner-Aus,0003=Schließer
                rawData.MqttValue = indexData.ValueList.FirstOrDefault(x => x.StartsWith(payloadData)).Substring(5);
            }
            else if (!noUnit)
                rawData.MqttValue = indexData.Converter.ConvertValue(payloadData) + indexData.Unit;
            else
                rawData.MqttValue = indexData.Converter.ConvertValue(payloadData);

            return rawData;
        }

        public string TranslateBack(string topic, string value, string senderId)
        {
            //remove the /set
            if (topic.EndsWith("/set"))
                topic = topic.Substring(0, topic.Length - 4);

            //remove the first topic because it is the custom one from the config file and not in the ElsterTable config file            
            topic = topic.Substring(topic.IndexOf('/')+1);

            //check the sender length. Fail in case of unexpected length
            if (senderId.Length != 3)
            {
                Console.WriteLine("The senderID is not exatly 3.");
                return "";
            }


            // Get the Elster Index by the topic
            var elsterTable = new ElsterIndex();
            var elsterItem = elsterTable.ElsterIndexTable.FirstOrDefault(x => x.MqttTopic == topic);

            //Index not available
            if (elsterItem == null)
            {
                Console.WriteLine("Cannot find a Elster item that has the MQTT topic {0}", topic);
                return "";
            }

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

            var conv = elsterItem.Converter;
            var hexPayload = conv.ConvertValueBack(value);

            // The first 3 characters are the sender Id
            // followed by #
            // followed by Receiver Index ID  (i.e. 3 for 180 or higher)
            // followed by 0 (which means it is a write to the Canbus)
            // followed by 00 which is the offset of the receiver ID.
            // followed by FA to indicate the usage of the Elster Indexes
            // followed by the elster Index ID
            // followed by the value
            var canFrameString = string.Format("{0}#{1}0{2}FA{3}{4}", senderId, receiverId[0], receiverId[1], elsterItem.Index.ToString("X4"), hexPayload);
            

            //Verify Format of the translated back data
            if (string.IsNullOrEmpty(canFrameString) || canFrameString.Length != 14)
            {
                Console.WriteLine("Data is not lenght of 14: {0}", canFrameString);
                return "";
            }

            return canFrameString;
        }
    }
}
