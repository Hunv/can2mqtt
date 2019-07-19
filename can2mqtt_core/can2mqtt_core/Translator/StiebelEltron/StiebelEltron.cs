// Thanks to:
// http://juerg5524.ch/list_data.php
// https://wiki.c3re.de/index.php/Projekt_23_Smarthome_/_Zugriff_Heizung


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace can2mqtt_core.Translator.StiebelEltron
{
    /// <summary>
    /// This Translator class translates the CAN Bus data to values
    /// Validated with:
    /// - Stiebel Eltron LWZ 504
    /// </summary>
    public class StiebelEltron
    {
        public CanFrame Translate(CanFrame rawData, bool noUnit)
        {
            //Check if format is correct
            if (string.IsNullOrEmpty(rawData.PayloadCanData) || rawData.PayloadCanData.Length != 14)
            {
                Console.WriteLine("Data is not lenght of 14: {0}", rawData.PayloadCanData);
                return rawData;
            }

            //0 000 - direkt
            //3 180 - Kessel
            //  280 - atez
            //6 300, 301 ... - Control Unit / Bedienmodule(bei mir 301, 302 und 303)
            //  400 - Raumfernfühler
            //9 480 - Manager
            //A 500 - Heizmodul
            //  580 - Buskoppler
            //C 600, 601 ... -  Mischermodule(bei mir 601, 602, 603) (wpwm controller)
            //D 680 - PC(ComfortSoft) (6A1, 6A2)
            //  700 - Fremdgerät
            //  780 - DCF-Modul


            //- ModulType auf der Basis des ComfortSoft-Protokolls, 2. Byte (siehe robots, haustechnikdialog):
            //0 - write
            //1 - read
            //2 - response
            //3 - ack
            //4 - write ack
            //5 - write respond
            //6 - system
            //7 - system respond
            //20/21 (hex.) - write/read large telegram

            var payloadIndex = Convert.ToInt32(rawData.PayloadCanData.Substring(6, 4), 16);
            var payloadData = rawData.PayloadCanData.Substring(10);

            //Get IndexData
            var indexData = ElsterIndex.ElsterTable.FirstOrDefault(x => x.Index == payloadIndex);

            //Index not available
            if (indexData == null)
                return rawData;

            rawData.MqttTopicExtention = indexData.MqttTopic;

            if (!noUnit)
                rawData.MqttValue = indexData.Converter.ConvertValue(payloadData) + indexData.Unit;
            else
                rawData.MqttValue = indexData.Converter.ConvertValue(payloadData);

            return rawData;
        }

    }
}
