using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using MQTTnet.Server;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace can2mqtt
{
    public class Can2Mqtt : BackgroundService
    {
        private readonly ILogger<Can2Mqtt> Logger;
        private IMqttClient MqttClient;
        private IMqttClientOptions MqttClientOptions;
        private string CanTranslator;
        private string CanServer;
        private int CanServerPort = 29536;
        private bool CanForwardWrite = true;
        private bool CanForwardRead = false;
        private bool CanForwardResponse = true;
        private int CanReceiveBufferSize = 48;
        private string CanSenderId;
        private bool NoUnit = false;
        private string MqttTopic = "";
        private string MqttUser;
        private string MqttPassword;
        private string MqttServer;
        private string MqttClientId;
        private bool MqttAcceptSet = false;
        private NetworkStream TcpCanStream;
        private TcpClient ScdClient = null;
        private Translator.StiebelEltron.StiebelEltron Translator = null;
        private string Language = "EN";


        public Can2Mqtt(ILogger<Can2Mqtt> logger)
        {
            Logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                Logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                await Task.Delay(1000, stoppingToken);
            }
        }

        /// <summary>
        /// Loads the config.json to the local variables.
        /// </summary>
        private bool LoadConfig()
        {
            if (!File.Exists("./config.json"))
            {
                Console.WriteLine("Cannot find config.json. Copy and rename the config-sample.json and adjust your settings in that config file.");
                return false;
            }

            var jsonString = File.ReadAllText("config.json");
            var config = JsonNode.Parse(jsonString);

            if (config == null)
            {
                Console.WriteLine("Unable to read config file.");
                return false;
            }

            // Read the config file
            MqttTopic = Convert.ToString(config["MqttTopic"]);
            CanTranslator = Convert.ToString(config["MqttTranslator"]);
            CanForwardWrite = bool.Parse(config["CanForwardWrite"].ToString());
            CanForwardRead = bool.Parse(config["CanForwardRead"].ToString());
            CanForwardResponse = Convert.ToBoolean(config["CanForwardResponse"].ToString());
            NoUnit = Convert.ToBoolean(config["NoUnits"].ToString());
            CanReceiveBufferSize = Convert.ToInt32(config["CanReceiveBufferSize"].ToString());
            MqttUser = Convert.ToString(config["MqttUser"]);
            MqttPassword = Convert.ToString(config["MqttPassword"]);
            MqttClientId = Convert.ToString(config["MqttClientId"]);
            MqttServer = Convert.ToString(config["MqttServer"]);
            CanServer = Convert.ToString(config["CanServer"]);
            CanServerPort = Convert.ToInt32(config["CanServerPort"].ToString());
            MqttAcceptSet = Convert.ToBoolean(config["MqttAcceptSet"].ToString());
            CanSenderId = Convert.ToString(config["CanSenderId"]);
            Language = Convert.ToString(config["Language"]).ToUpper();

            return true;
        }
        
        /// <summary>
        /// Start the Can2Mqtt service.
        /// </summary>
        /// <param name="config">Commandline parameters</param>
        /// <returns></returns>
        public override async Task StartAsync(CancellationToken stoppingToken)
        {
            // Load the config from the config file
            if (LoadConfig() == false) {
                Console.WriteLine("Unable to load config successfully.");
                return;
            }
            
            // Create a new MQTT client.
            var mqttFactory = new MqttFactory();
            MqttClient = mqttFactory.CreateMqttClient();

            // Create TCP based options using the builder.
            MqttClientOptions = new MqttClientOptionsBuilder()
                .WithClientId(MqttClientId)
                .WithTcpServer(MqttServer)
                .WithCleanSession()
                .Build();

            // If authentication at the MQTT broker is enabled, create the options with credentials
            if (!string.IsNullOrEmpty(MqttUser) && MqttPassword != null)
            {
                Console.WriteLine("Connecting to MQTT broker using Credentials...");
                MqttClientOptions = new MqttClientOptionsBuilder()
                   .WithClientId(MqttClientId)
                   .WithTcpServer(MqttServer)
                   .WithCredentials(MqttUser, MqttPassword)
                   .WithCleanSession()
                   .Build();
            }

            //Handle reconnect on lost connection to MQTT Server
            MqttClient.UseDisconnectedHandler(async e =>
            {                
                Console.WriteLine("DISCONNECTED FROM MQTT BROKER {0} because of {1}", MqttServer, e.Reason);
                while (!MqttClient.IsConnected)
                {
                    try
                    {
                        // Connect the MQTT Client
                        await MqttClient.ConnectAsync(MqttClientOptions);
                        if (MqttClient.IsConnected)
                            Console.WriteLine("CONNECTED TO MQTT BROKER {0} using ClientId {1}", MqttServer, MqttClientId);
                        else
                            Console.WriteLine("CONNECTION TO MQTT BROKER {0} using ClientId {1} FAILED", MqttServer, MqttClientId);
                    }
                    catch (Exception ex) 
                    {
                        Console.WriteLine("RECONNECTING TO MQTT BROKER {0} FAILED. Exception: {1}", MqttServer, ex.ToString());
                        Thread.Sleep(10000); //Wait 10 seconds
                    }
                }
            });

            // Connect the MQTT Client to the MQTT Broker
            await MqttClient.ConnectAsync(MqttClientOptions);
            if (MqttClient.IsConnected)
                Console.WriteLine("CONNECTION TO MQTT BROKER {0} established using ClientId {1}", MqttServer, MqttClientId);

            // Only accept set commands, if they are enabled.
            if (MqttAcceptSet)
            {
                //Create listener on MQTT Broker to accept all messages with the MqttTopic from the config.
                await MqttClient.SubscribeAsync(new MQTTnet.Client.Subscribing.MqttClientSubscribeOptionsBuilder().WithTopicFilter(MqttTopic + "/#").Build());
                MqttClient.UseApplicationMessageReceivedHandler(async e =>
                {
                    // Check if it is a set topic and handle only if so.
                    if (e.ApplicationMessage.Topic.EndsWith("/set"))
                    {
                        Console.Write("Received MQTT SET Message; Topic = {0}", e.ApplicationMessage.Topic);
                        if (e.ApplicationMessage.Payload != null)
                        {
                            Console.WriteLine($" and Payload = {Encoding.UTF8.GetString(e.ApplicationMessage.Payload)}");
                            await SendCan(e.ApplicationMessage.Topic, e.ApplicationMessage.Payload, CanServer, CanServerPort);
                        }
                        else
                        {
                            Console.WriteLine(" WITH NO PAYLOAD");
                        }
                    }
                    // Check if it is a read topic. If yes, send a READ via CAN bus for the corresponding value to trigger a send of the value via CAN bus
                    else if (e.ApplicationMessage.Topic.EndsWith("/read"))
                    {
                        Console.Write("Received MQTT READ Message; Topic = {0}", e.ApplicationMessage.Topic);
                        if (e.ApplicationMessage.Topic != null)
                        {
                            Console.WriteLine("");
                            await ReadCan(e.ApplicationMessage.Topic, CanServer, CanServerPort);
                        }
                        else
                        {
                            Console.WriteLine(" WITH NO TOPIC");
                        }
                    }
                });
            }

            //Start listening on socketcand port
            await TcpCanBusListener(CanServer, CanServerPort);
        }

        /// <summary>
        /// Sends a payload to the CAN bus
        /// </summary>
        /// <param name="topic">The MQTT Topic</param>
        /// <param name="payload">The Payload for the CAN bus</param>
        /// <param name="canServer">The CAN Server (where socketcand runs)</param>
        /// <param name="canPort">The CAN Server Port</param>
        /// <returns></returns>
        private async Task SendCan(string topic, byte[] payload, string canServer, int canPort)
        {
            try
            {
                await ConnectTcpCanBus(canServer, canPort);

                //Get the data
                var data = Encoding.UTF8.GetString(payload);

                //Convert the data to the required format
                var canFrame = Translator.TranslateBack(topic, data, CanSenderId, NoUnit, "0");

                //Convert data part of the can Frame to socketcand required format
                var canFrameDataPart = canFrame.Split("#")[1];
                var canFrameSdData = "";

                for (int i = 0; i < canFrameDataPart.Length; i +=2)
                {
                    canFrameSdData += Convert.ToInt32(canFrameDataPart.Substring(i, 2), 16).ToString("X1") + " ";
                }
                canFrameSdData = canFrameSdData.Trim();

                // < send can_id can_datalength [data]* >
                var canFrameSdCommand = string.Format("< send {0} {1} {2} >", CanSenderId, canFrameDataPart.Length / 2, canFrameSdData);
                Console.WriteLine("Sending CAN Frame: {0}", canFrameSdCommand);
                TcpCanStream.Write(Encoding.Default.GetBytes(canFrameSdCommand));

                // read the send data
                var canFrameSdDataVerify = "";

                for (int i = 0; i < canFrameDataPart.Length; i += 2)
                {
                    if (i != 0)
                        canFrameSdDataVerify += Convert.ToInt32(canFrameDataPart.Substring(i, 2), 16).ToString("X1") + " ";
                    else
                        canFrameSdDataVerify += Convert.ToInt32(canFrameDataPart.Substring(i, 1) + "1", 16).ToString("X1") + " ";
                }
                canFrameSdDataVerify = canFrameSdDataVerify.Trim();
                var canFrameSdCommandVerify = string.Format("< send {0} {1} {2} >", CanSenderId, canFrameDataPart.Length / 2, canFrameSdDataVerify);
                Console.WriteLine("Sending CAN Verify Frame: {0}", canFrameSdCommandVerify);
                TcpCanStream.Write(Encoding.Default.GetBytes(canFrameSdCommandVerify));
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to set value via CAN bus. Error: " + ex.ToString());
            }
        }

        /// <summary>
        /// Sends a read command to the CAN bus to request the send of the requested value from the bus
        /// </summary>
        /// <param name="topic">The MQTT Topic</param>
        /// <param name="canServer">The CAN Server (where socketcand runs)</param>
        /// <param name="canPort">The CAN Server Port</param>
        /// <returns></returns>
        private async Task ReadCan(string topic, string canServer, int canPort)
        {
            try
            {
                await ConnectTcpCanBus(canServer, canPort);

                //Convert the data to the required format
                var canFrame = Translator.TranslateBack(topic, null, CanSenderId, NoUnit, "1");

                //Convert data part of the can Frame to socketcand required format
                var canFrameDataPart = canFrame.Split("#")[1];
                var canFrameSdData = "";

                for (int i = 0; i < canFrameDataPart.Length; i += 2)
                {
                    canFrameSdData += Convert.ToInt32(canFrameDataPart.Substring(i, 2), 16).ToString("X1") + " ";
                }
                canFrameSdData = canFrameSdData.Trim();

                // < send can_id can_datalength [data]* >
                var canFrameSdCommand = string.Format("< send {0} {1} {2} >", CanSenderId, canFrameDataPart.Length / 2, canFrameSdData);
                Console.WriteLine("Sending CAN Frame: {0}", canFrameSdCommand);
                TcpCanStream.Write(Encoding.Default.GetBytes(canFrameSdCommand));

                // read the send data
                var canFrameSdDataVerify = "";

                for (int i = 0; i < canFrameDataPart.Length; i += 2)
                {
                    if (i != 0)
                        canFrameSdDataVerify += Convert.ToInt32(canFrameDataPart.Substring(i, 2), 16).ToString("X1") + " ";
                    else
                        canFrameSdDataVerify += Convert.ToInt32(canFrameDataPart.Substring(i, 1) + "1", 16).ToString("X1") + " ";
                }
                canFrameSdDataVerify = canFrameSdDataVerify.Trim();
                var canFrameSdCommandVerify = string.Format("< send {0} {1} {2} >", CanSenderId, canFrameDataPart.Length / 2, canFrameSdDataVerify);
                Console.WriteLine("Sending CAN Verify Frame: {0}", canFrameSdCommandVerify);
                TcpCanStream.Write(Encoding.Default.GetBytes(canFrameSdCommandVerify));
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to send a read via CAN bus. Error: " + ex.ToString());
            }
        }

        /// <summary>
        /// Connect or verify connection to socketcand
        /// </summary>
        /// <param name="canServer">socketcand server address</param>
        /// <param name="canPort">socketcand server port</param>
        /// <returns></returns>
        public async Task ConnectTcpCanBus(string canServer, int canPort)
        {
            if (ScdClient != null && ScdClient.Connected)
            {
                Console.WriteLine("Already connected to SocketCanD.");
                return;
            }

            //Create TCP Client for connection to socketcand (=scd)
            while (ScdClient == null || !ScdClient.Connected)
            {
                try
                {
                    ScdClient = new TcpClient(canServer, canPort);
                }
                catch (Exception ea)
                {
                    Console.WriteLine("FAILED TO CONNECT TO SOCKETCAND {1}. {0}. Retry...", ea.Message, canServer);
                }
                await Task.Delay(TimeSpan.FromSeconds(5));
            }

            Console.WriteLine("CONNECTED TO SOCKETCAND {0} ON PORT {1}", canServer, canPort);

            //Create TCP Stream to read the CAN Bus Data
            byte[] data = new byte[CanReceiveBufferSize];
            TcpCanStream = ScdClient.GetStream();
            int bytes = TcpCanStream.Read(data, 0, data.Length);


            if (Encoding.Default.GetString(data, 0, bytes) == "< hi >")
            {
                Console.WriteLine("Handshake successful. Opening CAN interface...");
                TcpCanStream.Write(Encoding.Default.GetBytes("< open slcan0 >"));

                bytes = TcpCanStream.Read(data, 0, data.Length);
                if (Encoding.Default.GetString(data, 0, bytes) == "< ok >")
                {
                    Console.WriteLine("Opening connection to slcan0 successful. Changing socketcand mode to raw...");
                    TcpCanStream.Write(Encoding.Default.GetBytes("< rawmode >"));

                    bytes = TcpCanStream.Read(data, 0, data.Length);
                    if (Encoding.Default.GetString(data, 0, bytes) == "< ok >")
                    {
                        Console.WriteLine("Change to rawmode successful");
                    }
                }
            }
        }

        /// <summary>
        /// Listen to the CAN Bus (via TCP) and generate MQTT Message if there is an update
        /// </summary>
        /// <param name="canServer">socketcand server address</param>
        /// <param name="canPort">socketcand server port</param>
        /// <returns></returns>
        public async Task TcpCanBusListener(string canServer, int canPort)
        {
            try
            {
                byte[] data = new byte[CanReceiveBufferSize];
                string responseData = string.Empty;
                var previousData = "";

                await ConnectTcpCanBus(canServer, canPort);
                int bytes = TcpCanStream.Read(data, 0, data.Length);

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
                        if (canFrame.CanFrameType == "0" && CanForwardWrite ||
                            canFrame.CanFrameType == "1" && CanForwardRead ||
                            canFrame.CanFrameType == "2" && CanForwardResponse)
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
                    bytes = TcpCanStream.Read(data, 0, data.Length);
                }
                //Close the TCP Stream
                ScdClient.Close();

                Console.WriteLine("Disconnected from canServer {0} Port {1}", canServer, canPort);
            }
            catch(Exception ea)
            {
                Console.WriteLine("Error while reading CanBus Server. {0}", ea);
            }
            finally
            {
                //Reconnect to the canlogserver but do not wait for this here to avoid infinite loops
                _ = TcpCanBusListener(canServer, canPort); //Reconnect
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
                if (!string.IsNullOrEmpty(CanTranslator))
                {
                    //choose the translator to use and translate the message if translator exists
                    switch (CanTranslator)
                    {
                        case "StiebelEltron":
                            Translator = new Translator.StiebelEltron.StiebelEltron();
                            canMsg = Translator.Translate(canMsg, NoUnit, Language);
                            break;
                    }
                }

                //Verify connection to MQTT Broker is established
                while (!MqttClient.IsConnected)
                {
                    Console.WriteLine("UNHANDLED DISCONNECT FROM MQTT BROKER");
                    while (!MqttClient.IsConnected)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(5));

                        try
                        {
                            await MqttClient.ConnectAsync(MqttClientOptions);
                            Console.WriteLine("CONNECTED TO MQTT BROKER");
                        }
                        catch
                        {
                            Console.WriteLine("RECONNECTING TO MQTT BROKER FAILED. Retrying...");
                        }
                    }
                }

                //Logoutput with or without translated MQTT message
                if (string.IsNullOrEmpty(canMsg.MqttValue))
                    Console.WriteLine("Sending MQTT Message: {0} and Topic {1}", canMsg.PayloadFull.Trim(), MqttTopic);
                else
                    Console.WriteLine("Sending MQTT Message: {0} and Topic {1}{2}", canMsg.MqttValue, MqttTopic, canMsg.MqttTopicExtention);

                //Create MQTT Message
                var message = new MqttApplicationMessageBuilder()
                    .WithTopic(MqttTopic + canMsg.MqttTopicExtention)
                    .WithPayload(string.IsNullOrEmpty(canMsg.MqttValue) ? canMsg.PayloadFull : canMsg.MqttValue)
                    .WithExactlyOnceQoS()
                    .WithRetainFlag()
                    .Build();

                //Publish MQTT Message
                await MqttClient.PublishAsync(message);
            }
            catch (Exception ea)
            {
                Console.WriteLine("ERROR while sending MQTT message. {0}", ea.ToString());
            }
        }

    }
}
