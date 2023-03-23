using Xunit;

namespace can2mqtt.Tests.Translator
{
    public class StiebelEltronTest
    {
        [Fact]
        public void ConvertDec_ConvertValue()
        {
            var converter = new can2mqtt.Translator.StiebelEltron.ConvertDec();

            var result = converter.ConvertValue("0000");
            Assert.Equal("0", result);

            result = converter.ConvertValue("0001");
            Assert.Equal("0,1", result);

            result = converter.ConvertValue("0010");
            Assert.Equal("1,6", result);

            result = converter.ConvertValue("0100");
            Assert.Equal("25,6", result);

            result = converter.ConvertValue("1000");
            Assert.Equal("409,6", result);

            result = converter.ConvertValue("FFFF");
            Assert.Equal("-0,1", result);

            result = converter.ConvertValue("0A41");
            Assert.Equal("262,5", result);

            result = converter.ConvertValue("006A");
            Assert.Equal("10,6", result);

            result = converter.ConvertValue("1200");
            Assert.Equal("460,8", result);

            result = converter.ConvertValue("FF00");
            Assert.Equal("-25,6", result);
        }
        [Fact]
        public void ConvertDec_ConvertValueBack()
        {
            var converter = new can2mqtt.Translator.StiebelEltron.ConvertDec();

            var result = converter.ConvertValueBack("0");
            Assert.Equal("0000", result);

            result = converter.ConvertValueBack("0,1");
            Assert.Equal("0001", result);

            result = converter.ConvertValueBack("1,6");
            Assert.Equal("0010", result);

            result = converter.ConvertValueBack("25,6");
            Assert.Equal("0100", result);

            result = converter.ConvertValueBack("409,6");
            Assert.Equal("1000", result);

            result = converter.ConvertValueBack("-0,1");
            Assert.Equal("FFFF", result);

            result = converter.ConvertValueBack("262,5");
            Assert.Equal("0A41", result);

            result = converter.ConvertValueBack("10,6");
            Assert.Equal("006A", result);

            result = converter.ConvertValueBack("460,8");
            Assert.Equal("1200", result);

            result = converter.ConvertValueBack("-25,6");
            Assert.Equal("FF00", result);
        }


        [Fact]
        public void ConvertLittleEndian_ConvertValue()
        {
            var converter = new can2mqtt.Translator.StiebelEltron.ConvertLittleEndian();

            var result = converter.ConvertValue("0000");
            Assert.Equal("0", result);

            result = converter.ConvertValue("0001");
            Assert.Equal("256", result);

            result = converter.ConvertValue("0010");
            Assert.Equal("4096", result);

            result = converter.ConvertValue("0100");
            Assert.Equal("1", result);

            result = converter.ConvertValue("1000");
            Assert.Equal("16", result);

            result = converter.ConvertValue("FFFF");
            Assert.Equal("65535", result);

            result = converter.ConvertValue("0A41");
            Assert.Equal("16650", result);

            result = converter.ConvertValue("006A");
            Assert.Equal("27136", result);

            result = converter.ConvertValue("1200");
            Assert.Equal("18", result);

            result = converter.ConvertValue("FF00");
            Assert.Equal("255", result);


        }
        [Fact]
        public void ConvertLittleEndian_ConvertValueBack()
        {
            var converter = new can2mqtt.Translator.StiebelEltron.ConvertLittleEndian();

            var result = converter.ConvertValueBack("0");
            Assert.Equal("0000", result);

            result = converter.ConvertValueBack("4");
            Assert.Equal("0400", result);

            result = converter.ConvertValueBack("22");
            Assert.Equal("1600", result);

            result = converter.ConvertValueBack("256");
            Assert.Equal("0001", result);

            result = converter.ConvertValueBack("4096");
            Assert.Equal("0010", result);

            result = converter.ConvertValueBack("1");
            Assert.Equal("0100", result);

            result = converter.ConvertValueBack("16");
            Assert.Equal("1000", result);

            result = converter.ConvertValueBack("65535");
            Assert.Equal("FFFF", result);

            result = converter.ConvertValueBack("16650");
            Assert.Equal("0A41", result);

            result = converter.ConvertValueBack("27136");
            Assert.Equal("006A", result);

            result = converter.ConvertValueBack("18");
            Assert.Equal("1200", result);

            result = converter.ConvertValueBack("255");
            Assert.Equal("FF00", result);
        }

        [Fact]
        public void ConvertLittleEndianDec_ConvertValue()
        {
            var converter = new can2mqtt.Translator.StiebelEltron.ConvertLittleEndianDec();

            var result = converter.ConvertValue("0000");
            Assert.Equal("0", result);

            result = converter.ConvertValue("0001");
            Assert.Equal("25,6", result);

            result = converter.ConvertValue("0010");
            Assert.Equal("409,6", result);

            result = converter.ConvertValue("0100");
            Assert.Equal("0,1", result);

            result = converter.ConvertValue("1000");
            Assert.Equal("1,6", result);

            result = converter.ConvertValue("FFFF");
            Assert.Equal("6553,5", result);

            result = converter.ConvertValue("0A41");
            Assert.Equal("1665", result);

            result = converter.ConvertValue("006A");
            Assert.Equal("2713,6", result);

            result = converter.ConvertValue("1200");
            Assert.Equal("1,8", result);

            result = converter.ConvertValue("FF00");
            Assert.Equal("25,5", result);


        }
        [Fact]
        public void ConvertLittleEndianDec_ConvertValueBack()
        {
            var converter = new can2mqtt.Translator.StiebelEltron.ConvertLittleEndianDec();

            var result = converter.ConvertValueBack("0");
            Assert.Equal("0000", result);

            result = converter.ConvertValueBack("0.4");
            Assert.Equal("0400", result);

            result = converter.ConvertValueBack("2.2");
            Assert.Equal("1600", result);

            result = converter.ConvertValueBack("25.6");
            Assert.Equal("0001", result);

            result = converter.ConvertValueBack("409.6");
            Assert.Equal("0010", result);

            result = converter.ConvertValueBack("0.1");
            Assert.Equal("0100", result);

            result = converter.ConvertValueBack("1.6");
            Assert.Equal("1000", result);

            result = converter.ConvertValueBack("6553.5");
            Assert.Equal("FFFF", result);

            result = converter.ConvertValueBack("1665.0");
            Assert.Equal("0A41", result);

            result = converter.ConvertValueBack("2713.6");
            Assert.Equal("006A", result);

            result = converter.ConvertValueBack("1.8");
            Assert.Equal("1200", result);

            result = converter.ConvertValueBack("25.5");
            Assert.Equal("FF00", result);
        }


        [Fact]
        public void ConvertTime_ConvertValue()
        {
            var converter = new can2mqtt.Translator.StiebelEltron.ConvertTime();

            var result = converter.ConvertValue("0000");
            Assert.Equal("00:00", result);

            result = converter.ConvertValue("0001");
            Assert.Equal("00:15", result);

            result = converter.ConvertValue("000A");
            Assert.Equal("02:30", result);

            result = converter.ConvertValue("0060");
            Assert.Equal("00:00", result);

            result = converter.ConvertValue("000F");
            Assert.Equal("03:45", result);

            result = converter.ConvertValue("0041");
            Assert.Equal("16:15", result);

            result = converter.ConvertValue("0010");
            Assert.Equal("04:00", result);
        }
        [Fact]
        public void ConvertTime_ConvertValueBack()
        {
            var converter = new can2mqtt.Translator.StiebelEltron.ConvertTime();

            var result = converter.ConvertValueBack("00:00");
            Assert.Equal("0000", result);

            result = converter.ConvertValueBack("00:15");
            Assert.Equal("0001", result);

            result = converter.ConvertValueBack("02:30");
            Assert.Equal("000A", result);

            result = converter.ConvertValueBack("24:00");
            Assert.Equal("0060", result);

            result = converter.ConvertValueBack("03:45");
            Assert.Equal("000F", result);

            result = converter.ConvertValueBack("16:15");
            Assert.Equal("0041", result);

            result = converter.ConvertValueBack("04:00");
            Assert.Equal("0010", result);
        }

        [Fact]
        public void ConvertTimeRangeLittleEndian_ConvertValue()
        {
            var converter = new can2mqtt.Translator.StiebelEltron.ConvertTimeRangeLittleEndian();

            var result = converter.ConvertValue("0000");
            Assert.Equal("00:00 - 00:00", result);

            result = converter.ConvertValue("0101");
            Assert.Equal("00:15 - 00:15", result);

            result = converter.ConvertValue("0A0A");
            Assert.Equal("02:30 - 02:30", result);

            result = converter.ConvertValue("6060");
            Assert.Equal("00:00 - 00:00", result);

            result = converter.ConvertValue("0F0F");
            Assert.Equal("03:45 - 03:45", result);

            result = converter.ConvertValue("0F41");
            Assert.Equal("16:15 - 03:45", result);

            result = converter.ConvertValue("1041");
            Assert.Equal("16:15 - 04:00", result);
        }
        [Fact]
        public void ConvertTimeRangeLittleEndian_ConvertValueBack()
        {
            var converter = new can2mqtt.Translator.StiebelEltron.ConvertTimeRangeLittleEndian();

            var result = converter.ConvertValueBack("00:00 - 00:00");
            Assert.Equal("0000", result);

            result = converter.ConvertValueBack("00:15 - 00:15");
            Assert.Equal("0101", result);

            result = converter.ConvertValueBack("02:30 - 02:30");
            Assert.Equal("0A0A", result);

            result = converter.ConvertValueBack("24:00 - 24:00");
            Assert.Equal("6060", result);

            result = converter.ConvertValueBack("03:45 - 03:45");
            Assert.Equal("0F0F", result);

            result = converter.ConvertValueBack("16:15 - 03:45");
            Assert.Equal("0F41", result);

            result = converter.ConvertValueBack("16:15 - 04:00");
            Assert.Equal("1041", result);
        }


        [Fact]
        public void ConvertTimeRange_ConvertValue()
        {
            var converter = new can2mqtt.Translator.StiebelEltron.ConvertTimeRange();

            var result = converter.ConvertValue("0000");
            Assert.Equal("00:00 - 00:00", result);

            result = converter.ConvertValue("0101");
            Assert.Equal("00:15 - 00:15", result);

            result = converter.ConvertValue("0A0A");
            Assert.Equal("02:30 - 02:30", result);

            result = converter.ConvertValue("6060");
            Assert.Equal("00:00 - 00:00", result);

            result = converter.ConvertValue("0F0F");
            Assert.Equal("03:45 - 03:45", result);

            result = converter.ConvertValue("0F41");
            Assert.Equal("03:45 - 16:15", result);

            result = converter.ConvertValue("1041");
            Assert.Equal("04:00 - 16:15", result);
        }
        [Fact]
        public void ConvertTimeRange_ConvertValueBack()
        {
            var converter = new can2mqtt.Translator.StiebelEltron.ConvertTimeRange();

            var result = converter.ConvertValueBack("00:00 - 00:00");
            Assert.Equal("0000", result);

            result = converter.ConvertValueBack("00:15 - 00:15");
            Assert.Equal("0101", result);

            result = converter.ConvertValueBack("02:30 - 02:30");
            Assert.Equal("0A0A", result);

            result = converter.ConvertValueBack("24:00 - 24:00");
            Assert.Equal("6060", result);

            result = converter.ConvertValueBack("03:45 - 03:45");
            Assert.Equal("0F0F", result);

            result = converter.ConvertValueBack("03:45 - 16:15");
            Assert.Equal("0F41", result);

            result = converter.ConvertValueBack("04:00 - 16:15");
            Assert.Equal("1041", result);
        }


        [Fact]
        public void ConvertDate_ConvertValue()
        {
            var converter = new can2mqtt.Translator.StiebelEltron.ConvertDate();

            var result = converter.ConvertValue("1204");
            Assert.Equal("18.04.", result);

            result = converter.ConvertValue("1304");
            Assert.Equal("19.04.", result);

            result = converter.ConvertValue("0A07");
            Assert.Equal("10.07.", result);

            result = converter.ConvertValue("0C02");
            Assert.Equal("12.02.", result);

            result = converter.ConvertValue("180C");
            Assert.Equal("24.12.", result);

            result = converter.ConvertValue("1F0C");
            Assert.Equal("31.12.", result);

            result = converter.ConvertValue("0101");
            Assert.Equal("01.01.", result);
        }
        [Fact]
        public void ConvertDate_ConvertValueBack()
        {
            var converter = new can2mqtt.Translator.StiebelEltron.ConvertDate();

            var result = converter.ConvertValueBack("18.04.");
            Assert.Equal("1204", result);

            result = converter.ConvertValueBack("19.04.");
            Assert.Equal("1304", result);

            result = converter.ConvertValueBack("10.07.");
            Assert.Equal("0A07", result);

            result = converter.ConvertValueBack("12.02.");
            Assert.Equal("0C02", result);

            result = converter.ConvertValueBack("24.12.");
            Assert.Equal("180C", result);

            result = converter.ConvertValueBack("31.12.");
            Assert.Equal("1F0C", result);

            result = converter.ConvertValueBack("01.01.");
            Assert.Equal("0101", result);
        }


        [Fact]
        public void ConvertByte_ConvertValue()
        {
            var converter = new can2mqtt.Translator.StiebelEltron.ConvertByte();

            var result = converter.ConvertValue("0000");
            Assert.Equal("0", result);

            result = converter.ConvertValue("0001");
            Assert.Equal("1", result);

            result = converter.ConvertValue("0005");
            Assert.Equal("5", result);

            result = converter.ConvertValue("000F");
            Assert.Equal("15", result);

            result = converter.ConvertValue("00FF");
            Assert.Equal("255", result);

            result = converter.ConvertValue("00A5");
            Assert.Equal("165", result);

            result = converter.ConvertValue("001A");
            Assert.Equal("26", result);
        }
        [Fact]
        public void ConvertByte_ConvertValueBack()
        {
            var converter = new can2mqtt.Translator.StiebelEltron.ConvertByte();

            var result = converter.ConvertValueBack("0");
            Assert.Equal("0000", result);

            result = converter.ConvertValueBack("1");
            Assert.Equal("0001", result);

            result = converter.ConvertValueBack("5");
            Assert.Equal("0005", result);

            result = converter.ConvertValueBack("15");
            Assert.Equal("000F", result);

            result = converter.ConvertValueBack("255");
            Assert.Equal("00FF", result);

            result = converter.ConvertValueBack("165");
            Assert.Equal("00A5", result);

            result = converter.ConvertValueBack("26");
            Assert.Equal("001A", result);
        }


        [Fact]
        public void ConvertDouble_ConvertValue()
        {
            var converter = new can2mqtt.Translator.StiebelEltron.ConvertDouble();

            var result = converter.ConvertValue("0000");
            Assert.Equal("0", result);

            result = converter.ConvertValue("0001");
            Assert.Equal("1", result);

            result = converter.ConvertValue("0005");
            Assert.Equal("5", result);

            result = converter.ConvertValue("000F");
            Assert.Equal("15", result);

            result = converter.ConvertValue("00FF");
            Assert.Equal("255", result);

            result = converter.ConvertValue("00A5");
            Assert.Equal("165", result);

            result = converter.ConvertValue("001A");
            Assert.Equal("26", result);

            result = converter.ConvertValue("0A1A");
            Assert.Equal("2586", result);

            result = converter.ConvertValue("F01A");
            Assert.Equal("61466", result);

            result = converter.ConvertValue("FFFF");
            Assert.Equal("65535", result);

            result = converter.ConvertValue("7E1A");
            Assert.Equal("32282", result);
        }
        [Fact]
        public void ConvertDouble_ConvertValueBack()
        {
            var converter = new can2mqtt.Translator.StiebelEltron.ConvertDouble();

            var result = converter.ConvertValueBack("0");
            Assert.Equal("0000", result);

            result = converter.ConvertValueBack("1");
            Assert.Equal("0001", result);

            result = converter.ConvertValueBack("5");
            Assert.Equal("0005", result);

            result = converter.ConvertValueBack("15");
            Assert.Equal("000F", result);

            result = converter.ConvertValueBack("255");
            Assert.Equal("00FF", result);

            result = converter.ConvertValueBack("165");
            Assert.Equal("00A5", result);

            result = converter.ConvertValueBack("26");
            Assert.Equal("001A", result);

            result = converter.ConvertValueBack("2586");
            Assert.Equal("0A1A", result);

            result = converter.ConvertValueBack("61466");
            Assert.Equal("F01A", result);

            result = converter.ConvertValueBack("65535");
            Assert.Equal("FFFF", result);

            result = converter.ConvertValueBack("32282");
            Assert.Equal("7E1A", result);
        }


        [Fact]
        public void ConvertTriple_ConvertValue()
        {
            var converter = new can2mqtt.Translator.StiebelEltron.ConvertTriple();

            var result = converter.ConvertValue("0000");
            Assert.Equal("0", result);

            result = converter.ConvertValue("0001");
            Assert.Equal("1", result);

            result = converter.ConvertValue("0005");
            Assert.Equal("5", result);

            result = converter.ConvertValue("000F");
            Assert.Equal("15", result);

            result = converter.ConvertValue("00FF");
            Assert.Equal("255", result);

            result = converter.ConvertValue("00A5");
            Assert.Equal("165", result);

            result = converter.ConvertValue("001A");
            Assert.Equal("26", result);

            result = converter.ConvertValue("0A1A");
            Assert.Equal("2586", result);

            result = converter.ConvertValue("F01A");
            Assert.Equal("61466", result);

            result = converter.ConvertValue("FFFF");
            Assert.Equal("65535", result);

            result = converter.ConvertValue("7E1A");
            Assert.Equal("32282", result);
        }
        [Fact]
        public void ConvertTriple_ConvertValueBack()
        {
            var converter = new can2mqtt.Translator.StiebelEltron.ConvertTriple();

            var result = converter.ConvertValueBack("0");
            Assert.Equal("0000", result);

            result = converter.ConvertValueBack("1");
            Assert.Equal("0001", result);

            result = converter.ConvertValueBack("5");
            Assert.Equal("0005", result);

            result = converter.ConvertValueBack("15");
            Assert.Equal("000F", result);

            result = converter.ConvertValueBack("255");
            Assert.Equal("00FF", result);

            result = converter.ConvertValueBack("165");
            Assert.Equal("00A5", result);

            result = converter.ConvertValueBack("26");
            Assert.Equal("001A", result);

            result = converter.ConvertValueBack("2586");
            Assert.Equal("0A1A", result);

            result = converter.ConvertValueBack("61466");
            Assert.Equal("F01A", result);

            result = converter.ConvertValueBack("65535");
            Assert.Equal("FFFF", result);

            result = converter.ConvertValueBack("32282");
            Assert.Equal("7E1A", result);
        }



        [Fact]
        public void ConvertCent_ConvertValue()
        {
            var converter = new can2mqtt.Translator.StiebelEltron.ConvertCent();

            var result = converter.ConvertValue("0000");
            Assert.Equal("0", result);

            result = converter.ConvertValue("0001");
            Assert.Equal("0.01", result);

            result = converter.ConvertValue("0005");
            Assert.Equal("0.05", result);

            result = converter.ConvertValue("000F");
            Assert.Equal("0.15", result);

            result = converter.ConvertValue("00FF");
            Assert.Equal("2.55", result);

            result = converter.ConvertValue("00A5");
            Assert.Equal("1.65", result);

            result = converter.ConvertValue("001A");
            Assert.Equal("0.26", result);

            result = converter.ConvertValue("0A1A");
            Assert.Equal("25.86", result);

            result = converter.ConvertValue("F01A");
            Assert.Equal("614.66", result);

            result = converter.ConvertValue("FFFF");
            Assert.Equal("655.35", result);

            result = converter.ConvertValue("7E1A");
            Assert.Equal("322.82", result);

            result = converter.ConvertValue("0030");
            Assert.Equal("0.48", result);
        }
        [Fact]
        public void ConvertCent_ConvertValueBack()
        {
            var converter = new can2mqtt.Translator.StiebelEltron.ConvertCent();

            var result = converter.ConvertValueBack("0");
            Assert.Equal("0000", result);

            result = converter.ConvertValueBack("0,01");
            Assert.Equal("0001", result);

            result = converter.ConvertValueBack("0,05");
            Assert.Equal("0005", result);

            result = converter.ConvertValueBack("0,15");
            Assert.Equal("000F", result);

            result = converter.ConvertValueBack("2,55");
            Assert.Equal("00FF", result);

            result = converter.ConvertValueBack("1,65");
            Assert.Equal("00A5", result);

            result = converter.ConvertValueBack("0,26");
            Assert.Equal("001A", result);

            result = converter.ConvertValueBack("25,86");
            Assert.Equal("0A1A", result);

            result = converter.ConvertValueBack("614,66");
            Assert.Equal("F01A", result);

            result = converter.ConvertValueBack("655,35");
            Assert.Equal("FFFF", result);

            result = converter.ConvertValueBack("322,82");
            Assert.Equal("7E1A", result);
        }



        [Fact]
        public void ConvertMille_ConvertValue()
        {
            var converter = new can2mqtt.Translator.StiebelEltron.ConvertMille();

            var result = converter.ConvertValue("0000");
            Assert.Equal("0", result);

            result = converter.ConvertValue("0001");
            Assert.Equal("0,001", result);

            result = converter.ConvertValue("0005");
            Assert.Equal("0,005", result);

            result = converter.ConvertValue("000F");
            Assert.Equal("0,015", result);

            result = converter.ConvertValue("00FF");
            Assert.Equal("0,255", result);

            result = converter.ConvertValue("00A5");
            Assert.Equal("0,165", result);

            result = converter.ConvertValue("001A");
            Assert.Equal("0,026", result);

            result = converter.ConvertValue("0A1A");
            Assert.Equal("2,586", result);

            result = converter.ConvertValue("F01A");
            Assert.Equal("61,466", result);

            result = converter.ConvertValue("FFFF");
            Assert.Equal("65,535", result);

            result = converter.ConvertValue("7E1A");
            Assert.Equal("32,282", result);
        }
        [Fact]
        public void ConvertMille_ConvertValueBack()
        {
            var converter = new can2mqtt.Translator.StiebelEltron.ConvertMille();

            var result = converter.ConvertValueBack("0");
            Assert.Equal("0000", result);

            result = converter.ConvertValueBack("0,001");
            Assert.Equal("0001", result);

            result = converter.ConvertValueBack("0,005");
            Assert.Equal("0005", result);

            result = converter.ConvertValueBack("0,015");
            Assert.Equal("000F", result);

            result = converter.ConvertValueBack("0,255");
            Assert.Equal("00FF", result);

            result = converter.ConvertValueBack("0,165");
            Assert.Equal("00A5", result);

            result = converter.ConvertValueBack("0,026");
            Assert.Equal("001A", result);

            result = converter.ConvertValueBack("2,586");
            Assert.Equal("0A1A", result);

            result = converter.ConvertValueBack("61,466");
            Assert.Equal("F01A", result);

            result = converter.ConvertValueBack("65,535");
            Assert.Equal("FFFF", result);

            result = converter.ConvertValueBack("32,282");
            Assert.Equal("7E1A", result);
        }



        [Fact]
        public void ConvertTimeDomain_ConvertValue()
        {
            var converter = new can2mqtt.Translator.StiebelEltron.ConvertTimeDomain();

            var result = converter.ConvertValue("1205");
            Assert.Equal("04:30 - 01:15", result);

            result = converter.ConvertValue("0000");
            Assert.Equal("00:00 - 00:00", result);

            result = converter.ConvertValue("0101");
            Assert.Equal("00:15 - 00:15", result);

            result = converter.ConvertValue("0A0A");
            Assert.Equal("02:30 - 02:30", result);

            result = converter.ConvertValue("6060");
            Assert.Equal("24:00 - 24:00", result);

            result = converter.ConvertValue("0F0F");
            Assert.Equal("03:45 - 03:45", result);

            result = converter.ConvertValue("0F41");
            Assert.Equal("03:45 - 16:15", result);

            result = converter.ConvertValue("1041");
            Assert.Equal("04:00 - 16:15", result);

        }
        [Fact]
        public void ConvertTimeDomain_ConvertValueBack()
        {
            var converter = new can2mqtt.Translator.StiebelEltron.ConvertTimeDomain();

            var result = converter.ConvertValueBack("04:30 - 01:15");
            Assert.Equal("1205", result);

            result = converter.ConvertValueBack("00:00 - 00:00");
            Assert.Equal("0000", result);

            result = converter.ConvertValueBack("00:15 - 00:15");
            Assert.Equal("0101", result);

            result = converter.ConvertValueBack("02:30 - 02:30");
            Assert.Equal("0A0A", result);

            result = converter.ConvertValueBack("24:00 - 24:00");
            Assert.Equal("6060", result);

            result = converter.ConvertValueBack("03:45 - 03:45");
            Assert.Equal("0F0F", result);

            result = converter.ConvertValueBack("03:45 - 16:15");
            Assert.Equal("0F41", result);

            result = converter.ConvertValueBack("04:00 - 16:15");
            Assert.Equal("1041", result);
        }


        [Fact]
        public void ConvertDefault_ConvertValue()
        {
            var converter = new can2mqtt.Translator.StiebelEltron.ConvertDefault();

            var result = converter.ConvertValue("0000");
            Assert.Equal("0", result);

            result = converter.ConvertValue("0001");
            Assert.Equal("1", result);

            result = converter.ConvertValue("0005");
            Assert.Equal("5", result);

            result = converter.ConvertValue("000F");
            Assert.Equal("15", result);

            result = converter.ConvertValue("00FF");
            Assert.Equal("255", result);

            result = converter.ConvertValue("00A5");
            Assert.Equal("165", result);

            result = converter.ConvertValue("001A");
            Assert.Equal("26", result);

            result = converter.ConvertValue("0A1A");
            Assert.Equal("2586", result);

            result = converter.ConvertValue("F01A");
            Assert.Equal("61466", result);

            result = converter.ConvertValue("FFFF");
            Assert.Equal("65535", result);

            result = converter.ConvertValue("7E1A");
            Assert.Equal("32282", result);
        }
        [Fact]
        public void ConvertDefault_ConvertValueBack()
        {
            var converter = new can2mqtt.Translator.StiebelEltron.ConvertDefault();

            var result = converter.ConvertValueBack("0");
            Assert.Equal("0000", result);

            result = converter.ConvertValueBack("1");
            Assert.Equal("0001", result);

            result = converter.ConvertValueBack("5");
            Assert.Equal("0005", result);

            result = converter.ConvertValueBack("15");
            Assert.Equal("000F", result);

            result = converter.ConvertValueBack("255");
            Assert.Equal("00FF", result);

            result = converter.ConvertValueBack("165");
            Assert.Equal("00A5", result);

            result = converter.ConvertValueBack("26");
            Assert.Equal("001A", result);

            result = converter.ConvertValueBack("2586");
            Assert.Equal("0A1A", result);

            result = converter.ConvertValueBack("61466");
            Assert.Equal("F01A", result);

            result = converter.ConvertValueBack("65535");
            Assert.Equal("FFFF", result);

            result = converter.ConvertValueBack("32282");
            Assert.Equal("7E1A", result);
        }


        [Fact]
        public void ConvertLanguage_ConvertValue()
        {
            var converter = new can2mqtt.Translator.StiebelEltron.ConvertLanguage();

            var result = converter.ConvertValue("0000");
            Assert.Equal("German", result);

            result = converter.ConvertValue("0100");
            Assert.Equal("English", result);

            result = converter.ConvertValue("0200");
            Assert.Equal("French", result);

            result = converter.ConvertValue("0300");
            Assert.Equal("Dutch", result);

            result = converter.ConvertValue("0400");
            Assert.Equal("Italian", result);

            result = converter.ConvertValue("0500");
            Assert.Equal("Swedish", result);

            result = converter.ConvertValue("0600");
            Assert.Equal("Polish", result);

            result = converter.ConvertValue("0700");
            Assert.Equal("Czech", result);

            result = converter.ConvertValue("0800");
            Assert.Equal("Hungarian", result);

            result = converter.ConvertValue("0900");
            Assert.Equal("Spanish", result);

            result = converter.ConvertValue("0A00");
            Assert.Equal("Finnish", result);

            result = converter.ConvertValue("0B00");
            Assert.Equal("Danish", result);

            result = converter.ConvertValue("0D00");
            Assert.Equal("Unknown (0D00)", result);
        }
        [Fact]
        public void ConvertLanguage_ConvertValueBack()
        {
            var converter = new can2mqtt.Translator.StiebelEltron.ConvertLanguage();


            var result = converter.ConvertValueBack("German");
            Assert.Equal("0000", result);

            result = converter.ConvertValueBack("English");
            Assert.Equal("0100", result);

            result = converter.ConvertValueBack("French");
            Assert.Equal("0200", result);

            result = converter.ConvertValueBack("Dutch");
            Assert.Equal("0300", result);

            result = converter.ConvertValueBack("Italian");
            Assert.Equal("0400", result);

            result = converter.ConvertValueBack("Swedish");
            Assert.Equal("0500", result);

            result = converter.ConvertValueBack("Polish");
            Assert.Equal("0600", result);

            result = converter.ConvertValueBack("Czech");
            Assert.Equal("0700", result);

            result = converter.ConvertValueBack("Hungarian");
            Assert.Equal("0800", result);

            result = converter.ConvertValueBack("Spanish");
            Assert.Equal("0900", result);

            result = converter.ConvertValueBack("Finnish");
            Assert.Equal("0A00", result);

            result = converter.ConvertValueBack("Danish");
            Assert.Equal("0B00", result);

            result = converter.ConvertValueBack("Kesuaheli");
            Assert.Equal("FFFF", result);
        }


        [Fact]
        public void ConvertBool_ConvertValue()
        {
            var converter = new can2mqtt.Translator.StiebelEltron.ConvertBool();

            var result = converter.ConvertValue("0000");
            Assert.Equal("false", result);

            result = converter.ConvertValue("0001");
            Assert.Equal("true", result);
        }
        [Fact]
        public void ConvertBool_ConvertValueBack()
        {
            var converter = new can2mqtt.Translator.StiebelEltron.ConvertBool();

            var result = converter.ConvertValueBack("false");
            Assert.Equal("0000", result);

            result = converter.ConvertValueBack("true");
            Assert.Equal("0001", result);
        }




        [Fact]
        public void ConvertBinary_ConvertValue()
        {
            var converter = new can2mqtt.Translator.StiebelEltron.ConvertBinary();

            var result = converter.ConvertValue("0000");
            Assert.Equal("0000000000000000", result);

            result = converter.ConvertValue("0001");
            Assert.Equal("0000000000000001", result);

            result = converter.ConvertValue("0200");
            Assert.Equal("0000001000000000", result);

            result = converter.ConvertValue("03A0");
            Assert.Equal("0000001110100000", result);

            result = converter.ConvertValue("0410");
            Assert.Equal("0000010000010000", result);

            result = converter.ConvertValue("FFFF");
            Assert.Equal("1111111111111111", result);

            result = converter.ConvertValue("A600");
            Assert.Equal("1010011000000000", result);

            result = converter.ConvertValue("0705");
            Assert.Equal("0000011100000101", result);

            result = converter.ConvertValue("FACC");
            Assert.Equal("1111101011001100", result);

            result = converter.ConvertValue("F482");
            Assert.Equal("1111010010000010", result);

            result = converter.ConvertValue("00A0");
            Assert.Equal("0000000010100000", result);

            result = converter.ConvertValue("ABBA");
            Assert.Equal("1010101110111010", result);

            result = converter.ConvertValue("EB7A");
            Assert.Equal("1110101101111010", result);
        }
        [Fact]
        public void ConvertBinary_ConvertValueBack()
        {
            var converter = new can2mqtt.Translator.StiebelEltron.ConvertBinary();


            var result = converter.ConvertValueBack("0000000000000000");
            Assert.Equal("0000", result);

            result = converter.ConvertValueBack("0000000000000001");
            Assert.Equal("0001", result);

            result = converter.ConvertValueBack("0000001000000000");
            Assert.Equal("0200", result);

            result = converter.ConvertValueBack("0000001110100000");
            Assert.Equal("03A0", result);

            result = converter.ConvertValueBack("0000010000010000");
            Assert.Equal("0410", result);

            result = converter.ConvertValueBack("1111111111111111");
            Assert.Equal("FFFF", result);

            result = converter.ConvertValueBack("1010011000000000");
            Assert.Equal("A600", result);

            result = converter.ConvertValueBack("0000011100000101");
            Assert.Equal("0705", result);

            result = converter.ConvertValueBack("1111101011001100");
            Assert.Equal("FACC", result);

            result = converter.ConvertValueBack("1111010010000010");
            Assert.Equal("F482", result);

            result = converter.ConvertValueBack("0000000010100000");
            Assert.Equal("00A0", result);

            result = converter.ConvertValueBack("1010101110111010");
            Assert.Equal("ABBA", result);

            result = converter.ConvertValueBack("1110101101111010");
            Assert.Equal("EB7A", result);
        }


        [Fact]
        public void ConvertLittleBool_ConvertValue()
        {
            var converter = new can2mqtt.Translator.StiebelEltron.ConvertLittleBool();

            var result = converter.ConvertValue("0000");
            Assert.Equal("false", result);

            result = converter.ConvertValue("0100");
            Assert.Equal("true", result);
        }
        [Fact]
        public void ConvertLittleBool_ConvertValueBack()
        {
            var converter = new can2mqtt.Translator.StiebelEltron.ConvertLittleBool();

            var result = converter.ConvertValueBack("false");
            Assert.Equal("0000", result);

            result = converter.ConvertValueBack("true");
            Assert.Equal("0100", result);
        }
    }
}
