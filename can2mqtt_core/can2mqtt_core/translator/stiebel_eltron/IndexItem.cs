using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace can2mqtt.Translator.StiebelEltron
{
    internal class IndexItem
    {        
        public string Index { get; set; }
        public string Sender { get; set; }
        public string Receiver { get; set; }
        public string Unit { get; set; }
        public string Converter { get; set; }
        public string NameDE { get; set; }
        public string NameEN { get; set; }
        public string MqttTopic { get; set; }
        public string DescriptionDE { get; set; }
        public string DescriptionEN { get; set; }
        public string Default { get; set; }
        public string MinValue { get; set; }
        public string MaxValue { get; set; }
        public string ValuesDE { get; set; }
        public string ValuesEN { get; set; }
        public bool ReadOnly { get; set; }
    }
}
