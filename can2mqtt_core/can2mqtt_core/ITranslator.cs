using can2mqtt;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace can2mqtt
{
    public interface ITranslator
    {
        CanFrame Translate(CanFrame rawData, bool noUnit, string language);
        string TranslateBack(string mqttTopic, string value, string senderId, bool noUnit, string canOperation);
    }
}
