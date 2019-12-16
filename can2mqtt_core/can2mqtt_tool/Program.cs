using can2mqtt_core;
using System;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace can2mqtt_tool
{
    class Program
    {
        public static string _Translator = null;
        public static bool _OnlyUnkown = false;

        public static async Task Main(string[] args)
        {
            if (args.Length == 0)
            {
                ShowHelp();
                return;
            }

            string serverAddress = "";
            int serverPort = 28700;
            string calculate = null;

            for (var i = 0; i < args.Length; i++)
            {
                if (args[i].ToLower() == "--CanServer" || args[i].ToLower() == "-cs")
                    serverAddress = args[i + 1];
                else if (args[i].ToLower() == "--CanServerPort" || args[i].ToLower() == "-cp")
                    serverPort = Convert.ToInt32(args[i + 1]);
                else if (args[i].ToLower() == "--Translator" || args[i].ToLower() == "-t")
                    _Translator = args[i + 1];
                else if (args[i].ToLower() == "--OnlyUnknown" || args[i].ToLower() == "-u")
                    _OnlyUnkown = true;
                else if (args[i].ToLower() == "--Calculate" || args[i].ToLower() == "-c")
                    calculate = args[i + 1];

            }

            if (!string.IsNullOrWhiteSpace(calculate))
            {
                //Use Translator (if selected)
                if (!string.IsNullOrEmpty(_Translator))
                {
                    //choose the translator to use and translate the message if translator exists
                    switch (_Translator)
                    {
                        case "StiebelEltron":
                            var a = new can2mqtt_core.Translator.StiebelEltron.ConvertBool();
                            Console.WriteLine("Bool:\t\t{0}", a.ConvertValue(calculate));
                            var j = new can2mqtt_core.Translator.StiebelEltron.ConvertLittleBool();
                            Console.WriteLine("LitteBool:\t{0}", j.ConvertValue(calculate));
                            var h = new can2mqtt_core.Translator.StiebelEltron.ConvertDouble();
                            Console.WriteLine("Double:\t\t{0}", h.ConvertValue(calculate));
                            var o = new can2mqtt_core.Translator.StiebelEltron.ConvertTriple();
                            Console.WriteLine("Triple:\t\t{0}", o.ConvertValue(calculate));
                            var b = new can2mqtt_core.Translator.StiebelEltron.ConvertByte();
                            Console.WriteLine("Byte:\t\t{0}", b.ConvertValue(calculate));
                            var e = new can2mqtt_core.Translator.StiebelEltron.ConvertDec();
                            Console.WriteLine("Dec:\t\t{0}", e.ConvertValue(calculate));
                            var c = new can2mqtt_core.Translator.StiebelEltron.ConvertCent();
                            Console.WriteLine("Cent:\t\t{0}", c.ConvertValue(calculate));
                            var l = new can2mqtt_core.Translator.StiebelEltron.ConvertMille();
                            Console.WriteLine("Mille:\t\t{0}", l.ConvertValue(calculate));
                            var d = new can2mqtt_core.Translator.StiebelEltron.ConvertDatum();
                            Console.WriteLine("Datum:\t\t{0}", d.ConvertValue(calculate));
                            var f = new can2mqtt_core.Translator.StiebelEltron.ConvertDefault();
                            Console.WriteLine("Default:\t{0}", f.ConvertValue(calculate));
                            var g = new can2mqtt_core.Translator.StiebelEltron.ConvertDev();
                            Console.WriteLine("Dev:\t\t{0}", g.ConvertValue(calculate));
                            var k = new can2mqtt_core.Translator.StiebelEltron.ConvertLittleEndian();
                            Console.WriteLine("LittleEndian:\t{0}", k.ConvertValue(calculate));
                            var r = new can2mqtt_core.Translator.StiebelEltron.ConvertLittleEndianDec();
                            Console.WriteLine("Ltl.EndianDec:\t{0}", r.ConvertValue(calculate));
                            var m = new can2mqtt_core.Translator.StiebelEltron.ConvertSprache();
                            Console.WriteLine("Language:\t{0}", m.ConvertValue(calculate));
                            var n = new can2mqtt_core.Translator.StiebelEltron.ConvertTimeDomain();
                            Console.WriteLine("TimeDomain:\t{0}", n.ConvertValue(calculate));
                            var p = new can2mqtt_core.Translator.StiebelEltron.ConvertZeit();
                            Console.WriteLine("Time:\t\t{0}", p.ConvertValue(calculate));
                            var q = new can2mqtt_core.Translator.StiebelEltron.ConvertBetriebsart();
                            Console.WriteLine("OpStatus:\t{0}", q.ConvertValue(calculate));
                            var s = new can2mqtt_core.Translator.StiebelEltron.ConvertBinary();
                            Console.WriteLine("Binary:\t{0}", s.ConvertValue(calculate));
                            
                            //var i = new can2mqtt_core.Translator.StiebelEltron.ConvertErr();
                            //Console.WriteLine("Err:\t{0}", i.ConvertValue(calculate));

                            break;
                        default:
                            Console.WriteLine("Unknown Translator");
                            break;
                    }
                }
                else
                {
                    Console.WriteLine("TRANSLATOR PARAMETER MISSING");
                }

                return;
            }

            await ConnectTcpCanBus(serverAddress, serverPort);
        }

        /// <summary>
        /// Listen to the CAN Bus (via TCP) and generate MQTT Message if there is an update
        /// </summary>
        /// <param name="canServer"></param>
        /// <param name="canPort"></param>
        /// <returns></returns>
        public static async Task ConnectTcpCanBus(string canServer, int canPort)
        {
            try
            {
                //Create TCP Client for connection to canlogserver (=cls)
                TcpClient clsClient = null;
                while (clsClient == null || !clsClient.Connected)
                {
                    try
                    {
                        clsClient = new TcpClient(canServer, canPort);
                    }
                    catch (Exception ea)
                    {
                        Console.WriteLine("FAILED TO CONNECT TO CANLOGSERVER {1}. {0}. Retry...", ea.Message, canServer);
                    }
                    await Task.Delay(TimeSpan.FromSeconds(5));
                }

                Console.WriteLine("CONNECTED TO CANLOGSERVER {0} ON PORT {1}", canServer, canPort);

                //Create TCP Stream to read the CAN Bus Data
                NetworkStream stream = clsClient.GetStream();
                byte[] data = new Byte[46];
                String responseData = String.Empty;
                int bytes = stream.Read(data, 0, data.Length);
                var previousData = "";

                //Infinite Loop
                while (bytes > 0)
                {
                    //Get the string from the received bytes. The string contains "(1561746016.537099) slcan0 180#D03CFA01120B00"
                    responseData = System.Text.Encoding.ASCII.GetString(data, 0, bytes);

                    //Split by new line. canlogserver sends a line break after each CAN frame
                    foreach (var aData in responseData.Split('\n'))
                    {
                        //If there is nothing, ignore
                        if (aData.Length == 0)
                            continue;

                        //Each CAN frame is 45 characters and should start with a (. If not 45 chars, it is the first part of the frame received before.
                        if (aData.Length != 45 && aData.StartsWith("("))
                        {
                            //Store the data for next received packets to combine it.
                            previousData = aData;
                        }
                        else
                        {
                            //Create the CAN frame
                            var canFrame = new CanFrame
                            {
                                RawFrame = aData
                            };

                            //If the lenght is not 45 charactes and not starts with (, it is the second part of the frame stored above. Combine it and clear cache.
                            if (aData.Length != 45 && !aData.StartsWith("("))
                            {
                                canFrame.RawFrame = previousData + aData;
                                previousData = "";
                            }

                            //Use Translator (if selected)
                            if (!string.IsNullOrEmpty(_Translator))
                            {
                                //choose the translator to use and translate the message if translator exists
                                switch (_Translator)
                                {
                                    case "StiebelEltron":
                                        var translator = new can2mqtt_core.Translator.StiebelEltron.StiebelEltron();
                                        canFrame = translator.Translate(canFrame, false);
                                        break;
                                }
                            }

                            //Skip if everything is known and only unknown things should be shown
                            if (_OnlyUnkown && !string.IsNullOrWhiteSpace(canFrame.MqttValue) && !string.IsNullOrWhiteSpace(canFrame.MqttTopicExtention))
                            {
                                continue;
                            }

                            string frameTypeString = "";
                            switch(canFrame.CanFrameType)
                            {
                                case "0":
                                    frameTypeString = "wrte";
                                    break;
                                case "1":
                                    frameTypeString = "read";
                                    break;
                                case "2":
                                    frameTypeString = "answ";
                                    break;
                                default:
                                    frameTypeString = "????";
                                    break;
                            }

                            //Console.WriteLine("{0}", canFrame.RawFrame);
                            Console.WriteLine("{0} {1} {2} {3} {4} {5} => {6}\t{7}",
                                canFrame.PayloadSenderCanId, canFrame.PayloadReceiverCanId, frameTypeString,
                                canFrame.IndexTableIndex, canFrame.ValueIndex, canFrame.Value, canFrame.MqttValue, canFrame.MqttTopicExtention);
                        }
                    }
                    //Reset byte counter
                    bytes = 0;

                    //Get next packages from canlogserver
                    bytes = stream.Read(data, 0, data.Length);
                }
                //Close the TCP Stream
                clsClient.Close();

                Console.WriteLine("Disconnected from canServer {0} Port {1}", canServer, canPort);
            }
            catch (Exception ea)
            {
                Console.WriteLine("Error while reading CanBus Server. {0}", ea);
            }
            finally
            {
                //Reconnect to the canlogserver but do not wait for this here to avoid infinite loops
                _ = ConnectTcpCanBus(canServer, canPort); //Reconnect
            }
        }

        private static void ShowHelp()
        {
            Console.WriteLine("This tool is to analyze and debug CAN messages!");
            Console.WriteLine("Syntax: can2mqtt_tool --CanServer \"192.168.0.123\"");
            Console.WriteLine("Additional Parameters:");
            Console.WriteLine("--CanServerPort  Sets the Port of canlogserver");
            Console.WriteLine("--Translator     Use a translator. Valid value: StiebelEltron");
            Console.WriteLine("--OnlyUnknown    Only when Translator is set. Show only Unknown values.");
            Console.WriteLine("--SendFilter     Only show CAN ID(s), that were send by the given ID(s). Valid values: comma separated CAN IDs");
            Console.WriteLine("--ReceiveFilter  Only show CAN ID(s), that were send to the given ID(s). Valid values: comma separated CAN IDs");
            Console.WriteLine("--IndexFilter    Only show values, that are using the given Elster Index(es). Valid values: comma separated Elster Indexes");
            Console.WriteLine("--LogPath        If given, the output will be logged. Valid value: Path the a log file");
            Console.WriteLine("--Calculate      Calculates a raw value to all available value converters of a Translator");
        }
    }
}
