using System;
using System.Collections.Generic;
using System.Text;

namespace can2mqtt_core.Translator.StiebelEltron
{
    public interface IValueConverter
    {
        string ConvertValue(string hexData);
    }

    /// <summary>
    /// Converts Decimal values (et_dec_val)
    /// </summary>
    public class ConvertDec : IValueConverter
    {
        public string ConvertValue(string hexData)
        {
            return ((double)Convert.ToInt32(hexData, 16) / 10).ToString();
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
    }

    /// <summary>
    /// Converts values (et_zeit)
    /// </summary>
    public class ConvertZeit : IValueConverter
    {
        public string ConvertValue(string hexData)
        {
            var hour = Convert.ToInt32(hexData.Substring(2), 16);
            var min = Convert.ToInt32(hexData.Substring(0,2), 16);

            //Untested
            return (hour.ToString() + ":" + min.ToString());
        }
    }

    /// <summary>
    /// Converts values (et_datum)
    /// </summary>
    public class ConvertDatum : IValueConverter
    {
        public string ConvertValue(string hexData)
        {
            var month = Convert.ToInt32(hexData.Substring(2), 16);
            var day = Convert.ToInt32(hexData.Substring(0, 2), 16);

            //Untested
            return (day.ToString() + "." + month.ToString() + ".");
        }
    }

    /// <summary>
    /// Converts values (et_byte)
    /// </summary>
    public class ConvertByte : IValueConverter
    {
        public string ConvertValue(string hexData)
        {
            if (hexData != "0000")
                //C++ handling: sprintf(Val, "%d", (signed char)Value);
                return Convert.ToSByte(hexData).ToString();
                //throw new NotImplementedException("ConvertByte for Data " + hexData + " not implemented. Please report example values via Github!");
            else
                return "0";            
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
    }

    /// <summary>
    /// Converts values (et_cent_val)
    /// </summary>
    public class ConvertCent : IValueConverter
    {
        public string ConvertValue(string hexData)
        {
            return ((double)Convert.ToInt32(hexData, 16) / 100).ToString();
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
    }

    /// <summary>
    /// Converts values (et_dev_nr)
    /// </summary>
    public class ConvertDev : IValueConverter
    {
        public string ConvertValue(string hexData)
        {
            return (Convert.ToInt32(hexData, 16) + 1).ToString();
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

            var hour = block1 / 4;
            var minute = (block1 % 4) * 15;
            var day = block2 / 4;
            var month = 15 * (total % 4);

            //Untested
            return (hour.ToString() + ":" + minute.ToString() + "-" + day.ToString() + "." + month.ToString() + ".");            
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
    }

    /// <summary>
    /// Converts values (et_betriebsart)
    /// </summary>
    public class ConvertBetriebsart : IValueConverter
    {
        public string ConvertValue(string hexData)
        {
            switch (hexData)
            {
                case "0000": return "Notbetrieb";
                case "0100": return "Bereitschaft";
                case "0200": return "Automatik";
                case "0300": return "Tagbetrieb";
                case "0400": return "Absenkbetrieb";
                case "0500": return "Warmwasser";
                case "0B00": return "???";
                default: return "Unbekannt";
            }
        }
    }
    
    /// <summary>
     /// Converts values (et_betriebsart)
     /// </summary>
    public class ConvertSprache : IValueConverter
    {
        public string ConvertValue(string hexData)
        {
            switch (hexData)
            {
                case "0000": return "Deutsch";
                case "0100": return "Englisch";
                case "0200": return "Französisch";
                case "0300": return "Niederländisch"; //?
                case "0400": return "Italienisch"; //?
                case "0500": return "Schwedisch"; //?
                case "0600": return "Polnisch"; //?
                case "0700": return "Tschechisch"; //?
                case "0800": return "Ungarisch"; //?
                case "0900": return "Spanisch"; //?
                case "0A00": return "Finnisch"; //?
                case "0B00": return "Dänisch"; //?

                default: return "Unbekannt";
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
                case "0000": return "Aus";
                case "0001": return "Ein";
                default: return "???";
            }
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
                case "0000": return "Aus";
                case "0100": return "Ein";
                default: return "???";
            }
        }
    }
}
