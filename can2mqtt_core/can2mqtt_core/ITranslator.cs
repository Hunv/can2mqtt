namespace can2mqtt
{
    public interface ITranslator
    {
        IEnumerable<string> MqttTopicsToPoll { get; }
        CanFrame Translate(CanFrame rawData, bool noUnit, string language, bool convertUnknown);
        IEnumerable<string> TranslateBack(string mqttTopic, string value, string senderId, bool noUnit, string canOperation);
    }
}
