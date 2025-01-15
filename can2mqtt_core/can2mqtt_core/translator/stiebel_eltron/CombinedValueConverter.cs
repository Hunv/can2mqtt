namespace can2mqtt.Translator.StiebelEltron
{
    public interface ICombinedValueConverter
    {
        string ConvertValue(int index, string hexData);
        string CombineValues(params string[] values);
    }

    public abstract class CombinedValueConverterBase : ICombinedValueConverter
    {
        public abstract string ConvertValue(int index, string hexData);
        public abstract string CombineValues(params string[] values);

        protected static int ConvertToInt(string hexData) => Convert.ToInt32(hexData, 16);
        protected static double ConvertToDouble(string hexData) => Convert.ToDouble(ConvertToInt(hexData));
    }

    public class CombinedValueDefaultConverter(int integerIndex) : CombinedValueConverterBase
    {
        private int integerIndex = integerIndex;

        public override string ConvertValue(int index, string hexData)
        {
            var value = ConvertToInt(hexData);
            if (index == integerIndex)
            {
                value *= 1000;
            }
            return value.ToString();
        }

        public override string CombineValues(params string[] values)
        {
            return values.Sum(v => int.Parse(v)).ToString();
        }
    }

    public class CombinedValueDecConverter(int fractionalIndex) : CombinedValueConverterBase
    {
        private int fractionalIndex = fractionalIndex;

        public override string ConvertValue(int index, string hexData)
        {
            var value = ConvertToDouble(hexData);
            if (index == fractionalIndex)
            {
                value /= 1000;
            }
            return value.ToString();
        }

        public override string CombineValues(params string[] values)
        {
            return values.Sum(v => double.Parse(v)).ToString();
        }
    }
}