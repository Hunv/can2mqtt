using can2mqtt;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace can2mqtt_tool
{
    class Program
    {
        public static string _Translator = null;
        public static bool _OnlyUnkown = false;
        public static string _LogPath = null;
        public static List<string> _FrameTypeFilter = new List<string>{"0","1","2","3","4","5","6","7"}; //0,2,3,4,5,6,7
        public static List<string> _SenderFilter = null;
        public static List<string> _ReceiveFilter = null;
        public static List<string> _IndexFilter = null;
        public static int _CanReceiveBufferSize = 48;

        public static async Task Main(string[] args)
        {
            //-cs 192.168.82.10 -t StiebelEltron -l log.txt -s 180,301,302,6A0 -r 180,301,302,6A0 -u

            if (args.Length == 0)
            {
                ShowHelp();
                return;
            }
            
            string serverAddress = "";
            int serverPort = 29536;
            string calculate = null;

            for (var i = 0; i < args.Length; i++)
            {
                if (args[i].ToLower() == "--canserver" || args[i].ToLower() == "-cs")
                    serverAddress = args[i + 1];
                else if (args[i].ToLower() == "--canserverport" || args[i].ToLower() == "-cp")
                    serverPort = Convert.ToInt32(args[i + 1]);
                else if (args[i].ToLower() == "--translator" || args[i].ToLower() == "-t")
                    _Translator = args[i + 1];
                else if (args[i].ToLower() == "--onlyunknown" || args[i].ToLower() == "-u")
                    _OnlyUnkown = true;
                else if (args[i].ToLower() == "--calculate" || args[i].ToLower() == "-c")
                    calculate = args[i + 1];
                else if (args[i].ToLower() == "--logpath" || args[i].ToLower() == "-l")
                    _LogPath = args[i + 1];
                else if (args[i].ToLower() == "--senderfilter" || args[i].ToLower() == "-s")
                    _SenderFilter = new List<string>(args[i + 1].Split(new char[] { ',', ';' }));
                else if (args[i].ToLower() == "--receiverfilter" || args[i].ToLower() == "-r")
                    _ReceiveFilter = new List<string>(args[i + 1].Split(new char[] { ',', ';' }));
                else if (args[i].ToLower() == "--indexfilter" || args[i].ToLower() == "-i")
                    _IndexFilter = new List<string>(args[i + 1].Split(new char[] { ',', ';' }));
                else if (args[i].ToLower() == "--canreceivebuffersize" || args[i].ToLower() == "-b")
                    _CanReceiveBufferSize = Convert.ToInt32(args[i + 1]);
                else if (args[i].ToLower() == "--frametypefilter" || args[i].ToLower() == "-f")
                    _FrameTypeFilter = new List<string>(args[i + 1].Split(new char[] { ',', ';' }));


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
                            var a = new can2mqtt.Translator.StiebelEltron.ConvertBool();
                            Console.WriteLine("Bool:\t\t{0}", a.ConvertValue(calculate));
                            var j = new can2mqtt.Translator.StiebelEltron.ConvertLittleBool();
                            Console.WriteLine("LitteBool:\t{0}", j.ConvertValue(calculate));
                            var h = new can2mqtt.Translator.StiebelEltron.ConvertDouble();
                            Console.WriteLine("Double:\t\t{0}", h.ConvertValue(calculate));
                            var o = new can2mqtt.Translator.StiebelEltron.ConvertTriple();
                            Console.WriteLine("Triple:\t\t{0}", o.ConvertValue(calculate));
                            var b = new can2mqtt.Translator.StiebelEltron.ConvertByte();
                            Console.WriteLine("Byte:\t\t{0}", b.ConvertValue(calculate));
                            var e = new can2mqtt.Translator.StiebelEltron.ConvertDec();
                            Console.WriteLine("Dec:\t\t{0}", e.ConvertValue(calculate));
                            var c = new can2mqtt.Translator.StiebelEltron.ConvertCent();
                            Console.WriteLine("Cent:\t\t{0}", c.ConvertValue(calculate));
                            var l = new can2mqtt.Translator.StiebelEltron.ConvertMille();
                            Console.WriteLine("Mille:\t\t{0}", l.ConvertValue(calculate));
                            var d = new can2mqtt.Translator.StiebelEltron.ConvertDate();
                            Console.WriteLine("Datum:\t\t{0}", d.ConvertValue(calculate));
                            var f = new can2mqtt.Translator.StiebelEltron.ConvertDefault();
                            Console.WriteLine("Default:\t{0}", f.ConvertValue(calculate));
                            var k = new can2mqtt.Translator.StiebelEltron.ConvertLittleEndian();
                            Console.WriteLine("LittleEndian:\t{0}", k.ConvertValue(calculate));
                            var r = new can2mqtt.Translator.StiebelEltron.ConvertLittleEndianDec();
                            Console.WriteLine("Ltl.EndianDec:\t{0}", r.ConvertValue(calculate));
                            var m = new can2mqtt.Translator.StiebelEltron.ConvertLanguage();
                            Console.WriteLine("Language:\t{0}", m.ConvertValue(calculate));
                            var n = new can2mqtt.Translator.StiebelEltron.ConvertTimeDomain();
                            Console.WriteLine("TimeDomain:\t{0}", n.ConvertValue(calculate));
                            var p = new can2mqtt.Translator.StiebelEltron.ConvertTime();
                            Console.WriteLine("Time:\t\t{0}", p.ConvertValue(calculate));
                            var q = new can2mqtt.Translator.StiebelEltron.ConvertBinary();
                            Console.WriteLine("Binary:\t{0}", q.ConvertValue(calculate));
                            var s = new can2mqtt.Translator.StiebelEltron.ConvertBool();
                            Console.WriteLine("Bool:\t{0}", s.ConvertValue(calculate));
                            
                            //var i = new can2mqtt.Translator.StiebelEltron.ConvertErr();
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
                //Create TCP Client for connection to socketcand (=scd)
                TcpClient scdClient = null;
                while (scdClient == null || !scdClient.Connected)
                {
                    try
                    {
                        scdClient = new TcpClient(canServer, canPort);
                    }
                    catch (Exception ea)
                    {
                        await Log(string.Format("FAILED TO CONNECT TO SOCKETCAND {1}. {0}. Retry...", ea.Message, canServer));
                    }
                    await Task.Delay(TimeSpan.FromSeconds(5));
                }

                await Log(string.Format("CONNECTED TO SOCKETCAND {0} ON PORT {1}", canServer, canPort));

                //Create TCP Stream to read the CAN Bus Data
                NetworkStream stream = scdClient.GetStream();
                byte[] data = new Byte[_CanReceiveBufferSize];
                String responseData = String.Empty;
                int bytes = stream.Read(data, 0, data.Length);
                var previousData = "";

                if (Encoding.Default.GetString(data, 0, bytes) == "< hi >")
                {
                    Console.WriteLine("Handshake successful. Opening CAN interface...");
                    stream.Write(Encoding.Default.GetBytes("< open slcan0 >"));

                    bytes = stream.Read(data, 0, data.Length);
                    if (Encoding.Default.GetString(data, 0, bytes) == "< ok >")
                    {
                        Console.WriteLine("Opening connection to slcan0 successful. Changing socketcand mode to raw...");
                        stream.Write(Encoding.Default.GetBytes("< rawmode >"));

                        bytes = stream.Read(data, 0, data.Length);
                        if (Encoding.Default.GetString(data, 0, bytes) == "< ok >")
                        {
                            Console.WriteLine("Change to rawmode successful");
                        }
                    }
                }

                await Log(string.Format("DATE      TIME     SND REC MODE ID INDX VALU => DATA   MQTT TOPIC"), true);

                //Infinite Loop
                while (bytes > 0)
                {
                    //Get the string from the received bytes.
                    responseData = previousData + Encoding.ASCII.GetString(data, 0, bytes);

                    //Each received frame starts with "< frame " and ends with " >".
                    //Check if the current responseData starts with "< frame". If not, drop everything before
                    if (!responseData.StartsWith("< frame "))
                    {
                        if (responseData.Contains("< frame "))
                        {
                            //just take everything starting at "< frame "
                            Console.WriteLine("Dropping \"{0}\" because it is not expected at the beginning of a frame.", responseData.Substring(0, responseData.IndexOf("< frame ")));
                            responseData = responseData.Substring(responseData.IndexOf("< frame "));
                        }
                        else
                        {
                            //Drop everything
                            responseData = "";
                        }
                    }

                    //Check if the responData has a closing " >". If not, save data and go on reading.
                    if (responseData != "" && !responseData.Contains(" >"))
                    {
                        Console.WriteLine("No closing tag found. Save data and get next bytes.");
                        previousData = responseData;
                        continue;
                    }

                    //As long as full frames exist in responseData
                    while (responseData.Contains(" >"))
                    {
                        var frame = responseData.Substring(0, responseData.IndexOf(" >") + 2);

                        //Create the CAN frame
                        var canFrame = new CanFrame
                        {
                            RawFrame = frame
                        };

                        //Console.WriteLine("Received CAN Frame: {0}", canFrame.RawFrame);

                        //Use Translator (if selected)
                        if (!string.IsNullOrEmpty(_Translator))
                        {
                            //choose the translator to use and translate the message if translator exists
                            switch (_Translator)
                            {
                                case "StiebelEltron":
                                    var translator = new can2mqtt.Translator.StiebelEltron.StiebelEltron();
                                    canFrame = translator.Translate(canFrame, false, "en");
                                    break;
                            }
                        }

                        //Does a filter applies to this frame?
                        bool isFiltered = false;

                        //Skip if everything is known and only unknown things should be shown
                        if (_OnlyUnkown && !string.IsNullOrWhiteSpace(canFrame.MqttValue) && !string.IsNullOrWhiteSpace(canFrame.MqttTopicExtention))
                            isFiltered = true;

                        //Skip if Senderfilter applies
                        if (_SenderFilter != null && !_SenderFilter.Contains(canFrame.PayloadSenderCanId))
                            isFiltered = true;

                        //Skip if Receiverfilter applies
                        if (_ReceiveFilter != null && !_ReceiveFilter.Contains(canFrame.PayloadReceiverCanId))
                            isFiltered = true;

                        //Skip if Indexfilter applies
                        if (_IndexFilter != null && !_IndexFilter.Contains(canFrame.ValueIndex))
                            isFiltered = true;

                        //Skip of FrameTypeFilter applies
                        if (_FrameTypeFilter != null && !_FrameTypeFilter.Contains(canFrame.CanFrameType))
                            isFiltered = true;

                        if (!isFiltered)
                        {
                            string frameTypeString = "";
                            switch (canFrame.CanFrameType)
                            {
                                case "0":
                                    frameTypeString = "wrte";
                                    break;
                                case "1":
                                    frameTypeString = "read";
                                    break;
                                case "2":
                                    frameTypeString = "resp";
                                    break;
                                case "3":
                                    frameTypeString = "ack ";
                                    break;
                                case "4":
                                    frameTypeString = "wrak";
                                    break;
                                case "5":
                                    frameTypeString = "wrrp";
                                    break;
                                case "6":
                                    frameTypeString = "syst";
                                    break;
                                case "7":
                                    frameTypeString = "syrp";
                                    break;
                                default:
                                    frameTypeString = "????";
                                    break;
                            }

                            if (canFrame.CanFrameType == "1")
                            {
                                await Log(string.Format("{0} {1} {2} {3} {4} {5}    \t{7}",
                                    canFrame.PayloadSenderCanId, canFrame.PayloadReceiverCanId, frameTypeString,
                                    canFrame.IndexTableIndex, canFrame.ValueIndex, canFrame.Value, canFrame.MqttValue, canFrame.MqttTopicExtention));
                            }
                            else
                            {
                                await Log(string.Format("{0} {1} {2} {3} {4} {5} => {6}\t{7}",
                                    canFrame.PayloadSenderCanId, canFrame.PayloadReceiverCanId, frameTypeString,
                                    canFrame.IndexTableIndex, canFrame.ValueIndex, canFrame.Value, canFrame.MqttValue, canFrame.MqttTopicExtention));
                            }
                        }

                        responseData = responseData.Substring(responseData.IndexOf(" >") + 2);
                    }

                    //Save data handled at next read
                    previousData = responseData;

                    //Reset byte counter
                    bytes = 0;

                    //Get next packages from canlogserver
                    bytes = stream.Read(data, 0, data.Length);
                }
                //Close the TCP Stream
                scdClient.Close();

                await Log(string.Format("Disconnected from cansocked {0} Port {1}", canServer, canPort));
            }
            catch (Exception ea)
            {
                await Log(string.Format("Error while reading cansocked. {0}", ea));
            }
            finally
            {
                //Reconnect to the cansocked but do not wait for this here to avoid infinite loops
                _ = ConnectTcpCanBus(canServer, canPort); //Reconnect
            }
        }

        private static void ShowHelp()
        {
            Console.WriteLine("This tool is to analyze and debug CAN messages!");
            Console.WriteLine("Syntax: can2mqtt_tool --CanServer \"192.168.0.123\"");
            Console.WriteLine("Additional Parameters:");
            Console.WriteLine("--CanServerPort  Sets the Port of cansocked");
            Console.WriteLine("--Translator     Use a translator. Valid value: StiebelEltron");
            Console.WriteLine("--OnlyUnknown    Only when Translator is set. Show only Unknown values.");
            Console.WriteLine("--SenderFilter   Only show CAN ID(s), that were send by the given ID(s). Valid values: comma separated CAN IDs");
            Console.WriteLine("--ReceiverFilter Only show CAN ID(s), that were send to the given ID(s). Valid values: comma separated CAN IDs");
            Console.WriteLine("--IndexFilter    Only show values, that are using the given Elster Index(es). Valid values: comma separated Elster Indexes");
            Console.WriteLine("--LogPath        If given, the output will be logged. Valid value: Path the a log file");
            Console.WriteLine("--Calculate      Calculates a raw value to all available value converters of a Translator");
        }

        private static async Task Log(string text, bool dontLogToFile = false)
        {
            var logText = string.Format("{0} {1}", (dontLogToFile ? "" : DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss")), text);
            Console.WriteLine(logText);

            if (_LogPath != null && !dontLogToFile)
            {
                StreamWriter sW = new StreamWriter(_LogPath, true, System.Text.Encoding.Default);
                await sW.WriteLineAsync(logText);
                sW.Close();
            }
        }
    }
}
