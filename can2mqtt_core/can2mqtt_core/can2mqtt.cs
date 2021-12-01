using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using MQTTnet.Server;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        int _CanReceiveBufferSize = 48;

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
            _CanReceiveBufferSize = config.CanReceiveBufferSize;

            // Create a new MQTT client.
            var mqttFactory = new MqttFactory();
            _MqttClient = mqttFactory.CreateMqttClient();

            // Create TCP based options using the builder.
            _MqttClientOptions = new MqttClientOptionsBuilder()
                .WithClientId(config.MqttClientId)
                .WithTcpServer(config.MqttServer)
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

            //Create local MQTT Broker for write-Commands
            var mqttServer = new MqttFactory().CreateMqttServer();
            await mqttServer.StartAsync(new MqttServerOptions());

            //Create listener on local MQTT Broker(currently not like this)
            //await _MqttClient.SubscribeAsync(new MQTTnet.Client.Subscribing.MqttClientSubscribeOptionsBuilder().WithTopicFilter("#").Build());
            //_MqttClient.UseApplicationMessageReceivedHandler(e =>
            //{
            //    Console.WriteLine("### RECEIVED APPLICATION MESSAGE ###");
            //    Console.WriteLine($"+ Topic = {e.ApplicationMessage.Topic}");
            //    Console.WriteLine($"+ Payload = {Encoding.UTF8.GetString(e.ApplicationMessage.Payload)}");
            //    Console.WriteLine($"+ QoS = {e.ApplicationMessage.QualityOfServiceLevel}");
            //    Console.WriteLine($"+ Retain = {e.ApplicationMessage.Retain}");
            //    Console.WriteLine();

            //    Task.Run(() => _MqttClient.PublishAsync("hello/world"));
            //});

            //Start listening on socketcand port
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
                        Console.WriteLine("FAILED TO CONNECT TO SOCKETCAND {1}. {0}. Retry...", ea.Message, canServer);
                    }
                    await Task.Delay(TimeSpan.FromSeconds(5));
                }
                
                Console.WriteLine("CONNECTED TO SOCKETCAND {0} ON PORT {1}", canServer, canPort);

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

                        Console.WriteLine("Received CAN Frame: {0}", canFrame.RawFrame);
                        responseData = responseData.Substring(responseData.IndexOf(" >") + 2);

                        //If forwarding is disabled for this type of frame, ignore it. Otherwise send the Frame
                        if (canFrame.CanFrameType == "0" && _CanForwardWrite ||
                            canFrame.CanFrameType == "1" && _CanForwardRead ||
                            canFrame.CanFrameType == "2" && _CanForwardResponse)
                        {
                            //Sent the CAN frame via MQTT
                            await SendMQTT(canFrame);
                        }
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
