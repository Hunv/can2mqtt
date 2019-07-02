using System;
using System.Collections.Generic;
using System.Text;

namespace can2mqtt_core
{
    public class CanFrame
    {
        public string RawFrame { get; set; }

        /// <summary>
        /// Returns the transmitted timestamp extracted from RawMessage
        /// </summary>
        public long Timestamp { get { return Convert.ToInt64(RawFrame.Substring(0, RawFrame.IndexOf(' ')).Trim(new char[] { '(',')' })); } }

        /// <summary>
        /// Returns the transmitted adapter extracted from RawMessage
        /// </summary>
        public string Adapter { get { return RawFrame.Substring(RawFrame.IndexOf(' ') + 1, RawFrame.LastIndexOf(' ')); } }

        /// <summary>
        /// Returns the transmitted timestamp extracted from RawMessage
        /// </summary>
        public string PayloadFull { get { return RawFrame.Substring(RawFrame.IndexOf(' ', RawFrame.IndexOf(' ') +1) +1); } }

        /// <summary>
        /// Returns the CAN Bus Data Id of a received message
        /// </summary>
        public string PayloadSenderCanId
        {
            get
            {
                if (!PayloadFull.Contains('#'))
                    return "";

                return PayloadFull.Substring(0, PayloadFull.IndexOf('#'));
            }
        }

        /// <summary>
        /// Returns the CAN Bus Receiver Id of a received message
        /// </summary>
        public string PayloadReceiverCanId
        {
            get
            {
                var cat = PayloadCanData.Substring(0, 1);
                var mod = PayloadCanData.Substring(2, 2);
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

                return receiverId.ToString();                
            }
        }

        public string CanFrameType { get { return PayloadCanData.Substring(1, 1); } }

        /// <summary>
        /// Returns the CAN Bus Data of a recived Message
        /// </summary>
        public string PayloadCanData { get { return PayloadFull.Substring(PayloadFull.IndexOf('#') +1); } }


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
