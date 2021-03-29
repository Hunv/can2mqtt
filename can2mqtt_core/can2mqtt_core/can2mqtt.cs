using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace can2mqtt_core
{
    public class Can2Mqtt
    {
        IMqttClient _MqttClient = null;
        IMqttClientOptions _MqttClientOptions = null;
        string _MqttTopic = "";
        string _CanTranslator = null;
        bool _CanForwardWrite = true;
        bool _CanForwardRead = false;
        bool _CanForwardResponse = true;
        bool _NoUnits = false;
        string _CANInterface = "";

        public Can2Mqtt()
        {
        }

        /// <summary>
        /// Start the Can2Mqtt service.
        /// </summary>
        /// <param name="config">Commandline parameters</param>
        /// <returns></returns>
        public async Task Start(DaemonConfig config)
        {
            _MqttTopic = config.MqttTopic;
            _CanTranslator = config.MqttTranslator;
            _CanForwardWrite = config.CanForwardWrite;
            _CanForwardRead = config.CanForwardRead;
            _CanForwardResponse = config.CanForwardResponse;
            _NoUnits = config.NoUnits;
            _CANInterface = config.CANInterface == "auto" || config.CANInterface == "" ? "can" : config.CANInterface;

            NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces();
            
            var adapter_cnt = adapters.Length;


            foreach (NetworkInterface adapter in adapters)
            {
                IPInterfaceProperties properties = adapter.GetIPProperties();
                

                if (adapter.Name.Contains(_CANInterface))
                {
                    Console.WriteLine("Will use {0} interface.", adapter.Name);
                    _CANInterface = adapter.Name;
                    break;
                }

                adapter_cnt--;
            }

            if (adapter_cnt == 0)
            {
                Console.WriteLine("No matching CAN interface found in the system. Program abort...");
                return;
            }

            // Create a new MQTT client.
            var mqttFactory = new MqttFactory();
            _MqttClient = mqttFactory.CreateMqttClient();

            // Create TCP based options using the builder.
            _MqttClientOptions = new MqttClientOptionsBuilder()
                .WithClientId(config.MqttClientId)
                .WithTcpServer(config.MqttServer)
                //.WithCredentials("bud", "%spencer%")
                //.WithTls()
                .WithCleanSession()
                .Build();

            //Handle reconnect on loosing connection to MQTT Server
            _MqttClient.UseDisconnectedHandler(async e =>
            {
                Console.WriteLine("DISCONNECTED FROM MQTT BROKER {0}", config.MqttServer);
                while (!_MqttClient.IsConnected)
                {
                    try
                    {
                        await _MqttClient.ConnectAsync(_MqttClientOptions);
                        Console.WriteLine("CONNECTED TO MQTT BROKER {0} using ClientId {1}", config.MqttServer, config.MqttClientId);
                    }
                    catch
                    {
                        Console.WriteLine("RECONNECTING TO MQTT BROKER {0} FAILED", config.MqttServer);
                        System.Threading.Thread.Sleep(10000); //Wait 10 seconds
                    }
                }
            });

            //Connect the MQTT Client to the MQTT Broker
            await _MqttClient.ConnectAsync(_MqttClientOptions);
            if (_MqttClient.IsConnected)
                Console.WriteLine("CONNECTED TO MQTT BROKER {0} using ClientId {1}", config.MqttServer, config.MqttClientId);

            //Start listening on canlogservers port
            await ConnectTcpCanBus(config.CanServer, config.CanServerPort);
        }

        /// <summary>
        /// Listen to the CAN Bus (via TCP) and generate MQTT Message if there is an update
        /// </summary>
        /// <param name="canServer"></param>
        /// <param name="canPort"></param>
        /// <returns></returns>
        public async Task ConnectTcpCanBus(string canServer, int canPort)
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
                byte[] data = new Byte[44];
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
                        if (aData.Length != 43 && aData.StartsWith("("))
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
                            if (aData.Length != 43 && !aData.StartsWith("("))
                            {
                                canFrame.RawFrame = previousData + aData;
                                previousData = "";
                            }

                            Console.WriteLine("Received CAN Frame: {0}", canFrame.RawFrame);

                            //If forwarding is disabled for this type of frame, ignore it.
                            if (canFrame.CanFrameType == "0" && !_CanForwardWrite ||
                                canFrame.CanFrameType == "1" && !_CanForwardRead ||
                                canFrame.CanFrameType == "2" && !_CanForwardResponse)
                                continue;

                            //Sent the CAN frame via MQTT
                            await SendMQTT(canFrame);
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
            catch(Exception ea)
            {
                Console.WriteLine("Error while reading CanBus Server. {0}", ea);
            }
            finally
            {
                //Reconnect to the canlogserver but do not wait for this here to avoid infinite loops
                _ = ConnectTcpCanBus(canServer, canPort); //Reconnect
            }
        }

        /// <summary>
        /// Sends a CAN-Message as a MQTT message
        /// </summary>
        /// <param name="canMsg"></param>
        /// <returns></returns>
        public async Task SendMQTT(CanFrame canMsg)
        {
            try
            {
                //Check if there is payload
                if (canMsg.RawFrame.Trim().Length == 0)
                    return;

                //Use Translator (if selected)
                if (!string.IsNullOrEmpty(_CanTranslator))
                {
                    //choose the translator to use and translate the message if translator exists
                    switch (_CanTranslator)
                    {
                        case "StiebelEltron":
                            var translator = new Translator.StiebelEltron.StiebelEltron();
                            canMsg = translator.Translate(canMsg, _NoUnits);
                            break;
                    }
                }

                //Verify connection to MQTT Broker is established
                while (!_MqttClient.IsConnected)
                {
                    Console.WriteLine("UNHANDLED DISCONNECT FROM MQTT BROKER");
                    while (!_MqttClient.IsConnected)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(5));

                        try
                        {
                            await _MqttClient.ConnectAsync(_MqttClientOptions);
                            Console.WriteLine("CONNECTED TO MQTT BROKER");
                        }
                        catch
                        {
                            Console.WriteLine("RECONNECTING TO MQTT BROKER FAILED. Retrying...");
                        }
                    }
                }

                //Logoutput with or without tranlated MQTT message
                if (string.IsNullOrEmpty(canMsg.MqttValue))
                    Console.WriteLine("Sending MQTT Message: {0} and Topic {1}", canMsg.PayloadFull.Trim(), _MqttTopic);
                else
                    Console.WriteLine("Sending MQTT Message: {0} and Topic {1}{2}", canMsg.MqttValue, _MqttTopic, canMsg.MqttTopicExtention);

                //Create MQTT Message
                var message = new MqttApplicationMessageBuilder()
                    .WithTopic(_MqttTopic + canMsg.MqttTopicExtention)
                    .WithPayload(string.IsNullOrEmpty(canMsg.MqttValue) ? canMsg.PayloadFull : canMsg.MqttValue)
                    .WithExactlyOnceQoS()
                    .WithRetainFlag()
                    .Build();

                //Publish MQTT Message
                await _MqttClient.PublishAsync(message);
            }
            catch (Exception ea)
            {
                Console.WriteLine("ERROR while sending MQTT message. {0}", ea.ToString());
            }
        }

    }
}
