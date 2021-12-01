using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace can2mqtt_core
{
    public class CanFrame
    {
        //Example socketcand frame: "< frame 6A0 1630437901.513376 3100FA000E0000 >"
        //Example canlogserver frame: "(1561746016.537099) slcan0 180#D03CFA01120B00"

        private string _RawFrame = "";
        public string RawFrame {
            get { return _RawFrame; }
            set
            {
                _RawFrame = value;
                var dataFrame = value.Replace("< frame ",""); // Remove the leading part
                //Console.WriteLine("Dataframe: {0}", dataFrame);
                PayloadSenderCanId = dataFrame.Substring(0, dataFrame.IndexOf(' '));
                
                dataFrame = dataFrame.Substring(PayloadSenderCanId.Length + 1);
                //Console.WriteLine("Dataframe: {0}", dataFrame);
                Timestamp = Convert.ToInt64(dataFrame.Substring(0, 10), new CultureInfo("en-US"));

                dataFrame = dataFrame.Substring(18);
                //Console.WriteLine("Dataframe: {0}", dataFrame);
                PayloadFull = dataFrame.Substring(0, dataFrame.IndexOf(' '));

                Adapter = "Unknown"; //Currently not implemented                
            }
        }

        /// <summary>
        /// Returns the transmitted timestamp extracted from RawMessage in Unix Timestamp format (i.e. 1630437901.513376)
        /// </summary>
        public double Timestamp{ get; private set; }

        /// <summary>
        /// Returns the transmitted adapter extracted from RawMessage
        /// </summary>
        public string Adapter { get; private set; } 

        /// <summary>
        /// Returns the transmitted Payload extracted from RawMessage
        /// </summary>
        public string PayloadFull { get; private set; }

        /// <summary>
        /// Returns the CAN Bus Data Id of a received message
        /// </summary>
        public string PayloadSenderCanId { get; private set; }

        /// <summary>
        /// Returns the CAN Bus Receiver Id of a received message
        /// </summary>
        public string PayloadReceiverCanId
        {
            get
            {
                var cat = PayloadFull.Substring(0, 1);
                var mod = PayloadFull.Substring(2, 2);
                var receiverId = 0;

                switch(cat)
                {
                    case "3":
                        receiverId = 0x180;
                        break;
                    case "6":
                        receiverId = 0x300;
                        break;
                    case "9":
                        receiverId = 0x480;
                        break;
                    case "A":
                        receiverId = 0x500;
                        break;
                    case "C":
                        receiverId = 0x600;
                        break;
                    case "D":
                        receiverId = 0x680;
                        break;
                }

                receiverId += Convert.ToInt16(mod.Substring(0, 1),16) * 16 + Convert.ToInt16(mod.Substring(1, 1),16);

                return receiverId.ToString("X3");                
            }
        }

        public string CanFrameType { get { return PayloadFull.Substring(1, 1); } }

        /// <summary>
        /// Gets the IndexTable Index. Usually this is FA but FD was also discovered in the past
        /// </summary>
        public string IndexTableIndex { get { return PayloadFull.Substring(4,2); } }

        /// <summary>
        /// The Index the value belongs to
        /// </summary>
        public string ValueIndex { get { return PayloadFull.Substring(6, 4); } }

        /// <summary>
        /// The Value that is transmitted by this CAN frame
        /// </summary>
        public string Value { get { return PayloadFull.Substring(10, 4); } }

        /// <summary>
        /// In case a translator was used, the topic may become more specified
        /// </summary>
        public string MqttTopicExtention { get; set; }

        /// <summary>
        /// In case a translator was used, the value was extracted from the payload
        /// </summary>
        public string MqttValue { get; set; }
    }
}
