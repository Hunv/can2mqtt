using System;
using System.Collections.Generic;
using System.Text;

namespace can2mqtt.Translator.StiebelEltron
{
    public class Enumerations
    {
        public enum ProgramMode //ElsterIndex 0x0112
        {
            Emergency = 0,
            Standby = 1,
            Automatic = 2,
            DaytimeOperation = 3,
            NighttimeOperation = 4,
            WarmWater = 5
        }

        public enum Language
        {
            German = 0,
            English = 1,
            French = 2
        }
    }
}
