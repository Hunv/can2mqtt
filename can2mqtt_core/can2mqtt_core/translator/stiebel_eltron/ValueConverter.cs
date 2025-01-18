using System.Globalization;
using System.Text;

namespace can2mqtt.Translator.StiebelEltron
{
    public interface IValueConverter
    {
        string ConvertValue(string hexData);
        string ConvertValueBack(string value);
    }

    /// <summary>
    /// Converts Decimal values (et_dec_val)
    /// </summary>
    public class ConvertDec : IValueConverter
    {
        public string ConvertValue(string hexData)
        {
            var result = (double)Convert.ToInt16(hexData, 16) / 10;

            return result.ToString();
        }

        public string ConvertValueBack(string value)
        {
            //This is a signed value and used i.e. for temperatures. 
            //That means if from the binary view of this hex value the first value is a 1, it is a negative value. 
            //A leading 1 would mean that the values are higher than 32768 (here: 3276.8, because of division by 10).
            //6553 = -1°C
            //6553,5 exists, after that 0°C
            var numValue = Convert.ToDouble(value);

            if (numValue < 0)
            {
                //it is negative
                numValue += 6553.6;
            }

            return ((int)(numValue * 10)).ToString("X4");
        }
    }


    /// <summary>
    /// Converts Little Endian values (et_little_endian)
    /// </summary>
    public class ConvertLittleEndian : IValueConverter
    {
        public string ConvertValue(string hexData)
        {
            var result = 0;
            for (int i = 0; i < hexData.Length; i += 2)
            {
                var digit = Convert.ToInt32(hexData.Substring(i, 2), 16) * Math.Pow(16, i);
                result += Convert.ToInt32(digit);
            }
            return result.ToString();
        }

        public string ConvertValueBack(string value)
        {
            var number = Convert.ToInt32(value);
            var part1 = (number % 256).ToString("X2");
            var part2 = "00";

            //second pair of bytes is used
            if (number > 255)
            {
                part2 = (number / 256).ToString("X2");
            }

            return part1 + part2;
        }
    }

    /// <summary>
    /// Converts Little Endian values (et_little_endian) with one digit after comma
    /// </summary>
    public class ConvertLittleEndianDec : IValueConverter
    {
        public string ConvertValue(string hexData)
        {
            decimal result = 0;
            for (int i = 0; i < hexData.Length; i += 2)
            {
                var digit = Convert.ToInt32(hexData.Substring(i, 2), 16) * Math.Pow(16, i);
                result += Convert.ToInt32(digit);
            }
            result /= 10;
            return result.ToString();
        }

        public string ConvertValueBack(string value)
        {
            var number = (int)(Convert.ToDouble(value)*10);
            var part1 = ((number /10) % 256).ToString("X2");
            var part2 = "00";

            //second pair of bytes is used
            if (number > 255)
            {
                part2 = (number /10/ 256).ToString("X2");
            }

            return part1 + part2;
        }
    }

    /// <summary>
    /// Converts values. Each increase is 15 Minutes (0001 = 00:15).
    /// </summary>
    public class ConvertTime : IValueConverter
    {
        public string ConvertValue(string hexData)
        {
            var result = Convert.ToInt32(hexData, 16);

            var time = new TimeSpan(0, result * 15, 0);


            return time.Hours.ToString("D2") + ":" + time.Minutes.ToString("D2");
        }
        public string ConvertValueBack(string value)
        {
            //Input = hh:mm
            //Output = 15 minute frame of a day
            //00:00 = 0000
            //00:15 = 0001
            //00:30 = 0002
            //02:00 = 0008
            //04:00 = 000F
            //04:15 = 0010

            var hour = value.Substring(0, 2);
            var minutes = value.Substring(3, 2);

            var hourQuarters = Convert.ToInt32(hour) * 4;
            var minuteQuarters = (int)(Math.Floor(Convert.ToDouble(minutes) / 15));

            return "00"+(hourQuarters + minuteQuarters).ToString("X2");
        }
    }


    /// <summary>
    /// Converts values. 0100 = 00:00-00:15
    /// </summary>
    public class ConvertTimeRangeLittleEndian : IValueConverter
    {
        public string ConvertValue(string hexData)
        {
            var result1 = Convert.ToInt32(hexData.Substring(0,2), 16);
            var result2 = Convert.ToInt32(hexData.Substring(2,2), 16);

            var time1 = "---";
            var time2 = "---";
            if (result1 != 128)
                time1 = (new TimeSpan(0, result1 * 15, 0)).Hours.ToString("D2") + ":" + (new TimeSpan(0, result1 * 15, 0)).Minutes.ToString("D2");

            if (result2 != 128)
                time2 = (new TimeSpan(0, result2 * 15, 0)).Hours.ToString("D2") + ":" + (new TimeSpan(0, result2 * 15, 0)).Minutes.ToString("D2");

            return time2 + " - " + time1;
        }

        public string ConvertValueBack(string value)
        {
            // Input: 00:00 - 00:00 in a 15 mintes timerange
            // Output: XXYY => XX = Time1, YY = Time2

            var time1 = value.Substring(0, 5);
            var time2 = value.Substring(8, 5);


            var hour1 = time1.Substring(0, 2);
            var minutes1 = time1.Substring(3, 2);

            var hourQuarters1 = Convert.ToInt32(hour1) * 4;
            var minuteQuarters1 = (int)Math.Floor(Convert.ToDouble(minutes1) / 15);

            var hour2 = time2.Substring(0, 2);
            var minutes2 = time2.Substring(3, 2);

            var hourQuarters2 = Convert.ToInt32(hour2) * 4;
            var minuteQuarters2 = (int)Math.Floor(Convert.ToDouble(minutes2) / 15);

            return (hourQuarters2 + minuteQuarters2).ToString("X2") + (hourQuarters1 + minuteQuarters1).ToString("X2");
        }
    }

    /// <summary>
    /// Converts values. 0001 = 00:00-00:15
    /// </summary>
    public class ConvertTimeRange : IValueConverter
    {
        public string ConvertValue(string hexData)
        {
            var result1 = Convert.ToInt32(hexData.Substring(0, 2), 16);
            var result2 = Convert.ToInt32(hexData.Substring(2, 2), 16);

            var time1 = "---";
            var time2 = "---";
            if (result1 != 128)
                time1 = (new TimeSpan(0, result1 * 15, 0)).Hours.ToString("D2") + ":" + (new TimeSpan(0, result1 * 15, 0)).Minutes.ToString("D2");

            if (result2 != 128)
                time2 = (new TimeSpan(0, result2 * 15, 0)).Hours.ToString("D2") + ":" + (new TimeSpan(0, result2 * 15, 0)).Minutes.ToString("D2");

            return time1 + " - " + time2;
        }

        public string ConvertValueBack(string value)
        {
            // Input: 00:00 - 00:00 in a 15 mintes timerange
            // Output: XXYY => XX = Time1, YY = Time2

            var time1 = value.Substring(0, 5);
            var time2 = value.Substring(8, 5);


            var hour1 = time1.Substring(0, 2);
            var minutes1 = time1.Substring(3, 2);

            var hourQuarters1 = Convert.ToInt32(hour1) * 4;
            var minuteQuarters1 = (int)Math.Floor(Convert.ToDouble(minutes1) / 15);

            var hour2 = time2.Substring(0, 2);
            var minutes2 = time2.Substring(3, 2);

            var hourQuarters2 = Convert.ToInt32(hour2) * 4;
            var minuteQuarters2 = (int)Math.Floor(Convert.ToDouble(minutes2) / 15);

            return (hourQuarters1 + minuteQuarters1).ToString("X2") + (hourQuarters2 + minuteQuarters2).ToString("X2");
        }
    }

    /// <summary>
    /// Converts values (et_datum)
    /// </summary>
    public class ConvertDate : IValueConverter
    {
        public string ConvertValue(string hexData)
        {
            var month = Convert.ToInt32(hexData.Substring(2), 16);
            var day = Convert.ToInt32(hexData.Substring(0, 2), 16);

            //Untested
            return (day.ToString("D2") + "." + month.ToString("D2") + ".");
        }

        public string ConvertValueBack(string value)
        {
            //input: 18.04.
            //output: DDMM
            return Convert.ToInt32(value.Substring(0, 2)).ToString("X2") + Convert.ToInt32(value.Substring(3, 2)).ToString("X2");
        }
    }

    /// <summary>
    /// Converts values (et_byte)
    /// </summary>
    public class ConvertByte : IValueConverter
    {
        public string ConvertValue(string hexData)
        {
            if (!hexData.StartsWith("00")) //A byte cannot be larger than 255
            {
                return "255";
            }
            if (hexData != "0000")
                //C++ handling: sprintf(Val, "%d", (signed char)Value);
                return Convert.ToByte(int.Parse(hexData, System.Globalization.NumberStyles.HexNumber)).ToString();
            else
                return "0";            
        }

        public string ConvertValueBack(string value)
        {
            // Input: 75
            // Output: 4B

            return (Convert.ToInt32(value).ToString("X4"));
        }
    }

    /// <summary>
    /// Converts values (et_double_val)
    /// This value is always just an int but in several unit steps (i.e. Wh, kWh). The unit has to be maintained by the frontend/middleware.
    /// </summary>
    public class ConvertDouble : IValueConverter
    {
        public string ConvertValue(string hexData)
        {
            if (hexData != "0000")
                return Convert.ToInt32(hexData, 16).ToString();
                //throw new NotImplementedException("ConvertDouble for Data " + hexData + " not implemented. Please report example values via Github!");
            else
                return "0";
        }
        public string ConvertValueBack(string value)
        {
            return Convert.ToInt32(value).ToString("X4");
        }
    }

    /// <summary>
    /// Converts values (et_triple_val)
    /// This value is always just an int but in several unit steps (i.e. Wh, kWh, MWh). The unit has to be maintained by the frontend/middleware.
    /// </summary>
    public class ConvertTriple : IValueConverter
    {
        public string ConvertValue(string hexData)
        {
            if (hexData != "0000")
                return Convert.ToInt32(hexData, 16).ToString();
                //throw new NotImplementedException("ConvertTriple for Data " + hexData + " not implemented. Please report example values via Github!");
            else
                return "0";
        }
        public string ConvertValueBack(string value)
        {
            return Convert.ToInt32(value).ToString("X4");
        }
    }

    /// <summary>
    /// Converts values (et_cent_val)
    /// </summary>
    public class ConvertCent : IValueConverter
    {
        public string ConvertValue(string hexData)
        {
            return ((double)(Convert.ToInt32(hexData, 16) / (double)100)).ToString(CultureInfo.GetCultureInfo("EN-US"));
        }
        public string ConvertValueBack(string value)
        {
            var valNum = (int)Math.Round(Convert.ToDouble(value, CultureInfo.GetCultureInfo("EN-US")) * 100);
            return valNum.ToString("X4");
        }
    }

    /// <summary>
    /// Converts values (et_mil_val)
    /// </summary>
    public class ConvertMille : IValueConverter
    {
        public string ConvertValue(string hexData)
        {
            return ((double)Convert.ToInt32(hexData, 16) / 1000).ToString();
        }
        public string ConvertValueBack(string value)
        {
            var valNum = (int)Math.Round(Convert.ToDouble(value) * 1000);
            return valNum.ToString("X4");
        }
    }

    /// <summary>
    /// Converts values (et_err_nr)
    /// </summary>
    public class ConvertErr : IValueConverter
    {
        public string ConvertValue(string hexData)
        {
            throw new NotImplementedException("ConvertErr for Data " + hexData + " not implemented.");
        }
        public string ConvertValueBack(string value)
        {
            throw new NotImplementedException("ConvertErr for Data " + value + " not implemented.");
        }
    }

    /// <summary>
    /// Converts values (et_time_domain)
    /// </summary>
    public class ConvertTimeDomain : IValueConverter
    {
        public string ConvertValue(string hexData)
        {
            var block1 = Convert.ToInt32(hexData.Substring(0, 2), 16);
            var block2 = Convert.ToInt32(hexData.Substring(2), 16);
            var total = Convert.ToInt32(hexData, 16);

            var hour1 = block1 / 4;
            var minute1 = (block1 % 4) * 15;
            var hour2 = block2 / 4;
            var minute2 = 15 * (total % 4);

            return (hour1.ToString("D2") + ":" + minute1.ToString("D2") + " - " + hour2.ToString("D2") + ":" + minute2.ToString("D2"));
        }

        public string ConvertValueBack(string value)
        {
            var time1 = value.Substring(0, 5);
            var time2 = value.Substring(8, 5);

            var hour1 = time1.Substring(0, 2);
            var minutes1 = time1.Substring(3, 2);

            var hourQuarters1 = Convert.ToInt32(hour1) * 4;
            var minuteQuarters1 = (int)Math.Floor(Convert.ToDouble(minutes1) / 15);

            var hour2 = time2.Substring(0, 2);
            var minutes2 = time2.Substring(3, 2);

            var hourQuarters2 = Convert.ToInt32(hour2) * 4;
            var minuteQuarters2 = (int)Math.Floor(Convert.ToDouble(minutes2) / 15);

            return (hourQuarters1 + minuteQuarters1).ToString("X2") + (hourQuarters2 + minuteQuarters2).ToString("X2");
        }
    }

    /// <summary>
    /// Converts values (et_default)
    /// </summary>
    public class ConvertDefault : IValueConverter
    {
        public string ConvertValue(string hexData)
        {
            return Convert.ToInt32(hexData, 16).ToString();
        }
        public string ConvertValueBack(string value)
        {
            return Convert.ToInt32(value).ToString("X4");
        }
    }
    
    /// <summary>
     /// Converts values (et_betriebsart)
     /// </summary>
    public class ConvertLanguage : IValueConverter
    {
        public string ConvertValue(string hexData)
        {
            switch (hexData)
            {
                case "0000": return "German";
                case "0100": return "English";
                case "0200": return "French";
                case "0300": return "Dutch"; //?
                case "0400": return "Italian"; //?
                case "0500": return "Swedish"; //?
                case "0600": return "Polish"; //?
                case "0700": return "Czech"; //?
                case "0800": return "Hungarian"; //?
                case "0900": return "Spanish"; //?
                case "0A00": return "Finnish"; //?
                case "0B00": return "Danish"; //?

                default: return "Unknown (" + hexData + ")";
            }
        }
        public string ConvertValueBack(string value)
        {
            switch (value)
            {
                case "German": return "0000";
                case "English": return "0100";
                case "French": return "0200";
                case "Dutch": return "0300"; //?
                case "Italian": return "0400"; //?
                case "Swedish": return "0500"; //?
                case "Polish": return "0600"; //?
                case "Czech": return "0700"; //?
                case "Hungarian": return "0800"; //?
                case "Spanish": return "0900"; //?
                case "Finnish": return "0A00"; //?
                case "Danish": return "0B00"; //?

                default: return "FFFF"; // Unknown
            }
        }
    }

    /// <summary>
    /// Converts values (et_bool)
    /// </summary>
    public class ConvertBool : IValueConverter
    {
        public string ConvertValue(string hexData)
        {
            switch (hexData)
            {
                case "0000": return "false";
                case "0001": return "true";
                default: return "???";
            }
        }
        public string ConvertValueBack(string value)
        {
            switch (value.ToLower())
            {
                case "aus":
                case "off":
                case "false":
                    return "0000";
                case "ein":
                case "on":
                case "true":
                    return "0001";
                default: throw new NotImplementedException("Value " + value + " cannot be converted to a boolean hex representation");
            }
        }
    }

    /// <summary>
    /// Converts a Hex Value to a Binary value (i.e. for operating status)
    /// </summary>
    public class ConvertBinary : IValueConverter
    {
        public string ConvertValue(string hexData)
        {
            return Convert.ToString(Convert.ToInt64(hexData, 16), 2).PadLeft(16,'0');
        }
        public string ConvertValueBack(string value)
        {
            return Convert.ToInt64(value, 2).ToString("X4");
        }
    }

    /// <summary>
    /// Converts values (et_little_bool)
    /// </summary>
    public class ConvertLittleBool : IValueConverter
    {
        public string ConvertValue(string hexData)
        {
            switch (hexData)
            {
                case "0000": return "false";
                case "0100": return "true";
                default: return "???";
            }
        }
        public string ConvertValueBack(string value)
        {
            switch (value.ToLower())
            {
                case "aus":
                case "off":
                case "false":
                    return "0000";
                case "ein":
                case "on":
                case "true":
                    return "0100";
                default: throw new NotImplementedException("Value " + value + " cannot be converted to a boolean hex representation");
            }
        }
    }

    class FallbackValueConverter : IValueConverter
    {
        private struct Converter {
            public string Name {get; init; }
            public IValueConverter ValueConverter {get; init;}
        }

        private static readonly Converter[] Converters = [
            new Converter { Name = "binary", ValueConverter = new ConvertBinary() },
            new Converter { Name = "bool", ValueConverter = new ConvertBool() },
            new Converter { Name = "byte", ValueConverter = new ConvertByte() },
            new Converter { Name = "cent", ValueConverter = new ConvertCent() },
            new Converter { Name = "datum", ValueConverter = new ConvertDate() },
            new Converter { Name = "dec", ValueConverter = new ConvertDec() },
            new Converter { Name = "default", ValueConverter = new ConvertDefault() },
            new Converter { Name = "double", ValueConverter = new ConvertDouble() },
            new Converter { Name = "err", ValueConverter = new ConvertErr() },
            new Converter { Name = "littlebool", ValueConverter = new ConvertLittleBool() },
            new Converter { Name = "littleendian", ValueConverter = new ConvertLittleEndian() },
            new Converter { Name = "littleendiandec", ValueConverter = new ConvertLittleEndianDec() },
            new Converter { Name = "mille", ValueConverter = new ConvertMille() },
            new Converter { Name = "sprache", ValueConverter = new ConvertLanguage() },
            new Converter { Name = "time", ValueConverter = new ConvertTime() },
            new Converter { Name = "timedomain", ValueConverter = new ConvertTimeDomain() },
            new Converter { Name = "timerange", ValueConverter = new ConvertTimeRange() },
            new Converter { Name = "timerangelittleendian", ValueConverter = new ConvertTimeRangeLittleEndian() },
            new Converter { Name = "triple", ValueConverter = new ConvertTriple() },
            new Converter { Name = "Default", ValueConverter = new ConvertDefault() },
        ];

        public string ConvertValue(string hexData)
        {
            var sb = new StringBuilder();
            foreach(var converter in Converters) {
                try {
                    sb.AppendLine($"{converter.Name}: {converter.ValueConverter.ConvertValue(hexData)}");
                } catch(Exception) {}
            }
            return sb.ToString();
        }

        public string ConvertValueBack(string value)
        {
            throw new NotImplementedException();
        }
    }
}
