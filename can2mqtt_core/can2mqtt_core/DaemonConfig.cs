using System;
using System.Collections.Generic;
using System.Text;

namespace can2mqtt_core
{
    public class DaemonConfig
    {
        /// <summary>
        /// The name of the Daemon/Service
        /// </summary>
        public string DaemonName { get; set; } = "Can2Mqtt";

        /// <summary>
        /// The Address of the server where the application canlogserver is running on. Default address: 127.0.0.1
        /// </summary>        
        public string CanServer { get; set; } = "127.0.0.1";

        /// <summary>
        /// The CAN Server Port of canlogserver
        /// </summary>
        public int CanServerPort { get; set; } = 29536;

        /// <summary>
        /// The Server where the MQTT broker is running on
        /// </summary>
        public string MqttServer { get; set; }

        /// <summary>
        /// The Id of the client for MQTT
        /// </summary>
        public string MqttClientId { get; set; } = "Can2Mqtt";

        /// <summary>
        /// The Top level topic. This is the root branch and all value topics will be attached to this value.
        /// </summary>
        public string MqttTopic { get; set; } = "Can2Mqtt";

        /// <summary>
        /// In case MQTT should only broadcast numeric values without the units, set this to true
        /// </summary>
        public bool NoUnits { get; set; } = false;

        /// <summary>
        /// Should the application forward CAN write message?
        /// </summary>
        public bool CanForwardWrite { get; set; } = true;

        /// <summary>
        /// Should the application forward CAN Read messages?
        /// </summary>
        public bool CanForwardRead { get; set; } = false;

        /// <summary>
        /// Should the application forward CAN Resposne messages?
        /// </summary>
        public bool CanForwardResponse { get; set; } = true;

        /// <summary>
        /// Null if the raw data should be forwarded to MQTT. Otherwise the Translator name.
        /// </summary>
        public string MqttTranslator { get; set; }


        /// <summary>
        /// The size of the Buffer to receive CAN messages. For Stiebel Eltron CAN bus messages this is 48 byte. 
        /// If the size is wrong, the messages will be fragmented which requires to put the messages together again.
        /// This is handled by this application but it is nicer to have the correct size.
        /// </summary>
        public int CanReceiveBufferSize { get; set; } = 48;
    }
}
