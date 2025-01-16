using can2mqtt.Translator.StiebelEltron;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using MQTTnet.Server;
using System.Net.Sockets;
using System.Text;
using System.Text.Json.Nodes;

namespace can2mqtt
{
    public class Can2Mqtt : BackgroundService
    {
        private readonly ILogger Logger;
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
        private string CanInterfaceName = "slcan0";
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
        private bool ConvertUnknown = false;

        private bool AutoPolling = false;
        private int AutoPollingInterval = 120; // in seconds
        private int AutoPollingThrottle = 150; // in milliseconds
        private Task AutoPollingTask = null;

        private readonly ILoggerFactory LoggerFactory;

        public Can2Mqtt(ILoggerFactory loggerFactory)
        {
            LoggerFactory = loggerFactory;
            Logger = loggerFactory.CreateLogger("can2mqtt");
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
            try
            {
                if (!File.Exists("./config.json"))
                {
                    Logger.LogError("Cannot find config.json. Copy and rename the config-sample.json and adjust your settings in that config file.");
                    return false;
                }

                var jsonString = File.ReadAllText("config.json");
                var config = JsonNode.Parse(jsonString);
                if (config == null)
                {
                    Logger.LogError("Unable to read config file.");
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
                CanInterfaceName = Convert.ToString(config["CanInterfaceName"]);
                Language = Convert.ToString(config["Language"]).ToUpper();
                ConvertUnknown = bool.Parse(config["ConvertUnknown"].ToString());
                AutoPolling = bool.Parse(config["AutoPolling"].ToString());
                AutoPollingInterval = Convert.ToInt32(config["AutoPollingInterval"].ToString());
                AutoPollingThrottle = Convert.ToInt32(config["AutoPollingThrottle"].ToString());

                //choose the translator to use and translate the message if translator exists
                switch (CanTranslator)
                {
                    case "StiebelEltron":
                        Translator = new StiebelEltron(LoggerFactory);
                        break;
                }

                return true;
            }
            catch (Exception ex)
            {
                Logger.LogCritical(ex, "Unable to read config file.");
                return false;
            }
        }

        /// <summary>
        /// Start the Can2Mqtt service.
        /// </summary>
        /// <param name="config">Commandline parameters</param>
        /// <returns></returns>
        public override async Task StartAsync(CancellationToken stoppingToken)
        {
            // Load the config from the config file
            if (!LoadConfig())
            {
                Logger.LogCritical("Unable to load config successfully.");
                return;
            }

            await SetupMqtt();
            SetupAutoPolling(stoppingToken);

            //Start listening on socketcand port
            await TcpCanBusListener(CanServer, CanServerPort);
            AutoPollingTask.Wait();
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
            Logger.LogDebug("Sending write request for topic {0}", topic);

            try
            {
                //Get the data
                var data = Encoding.UTF8.GetString(payload);

                //Convert the data to the required format
                var canFrame = Translator.TranslateBack(topic, data, CanSenderId, NoUnit, "0");

                await SendCanFrame(canServer, canPort, canFrame);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to set value via CAN bus.");
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
            Logger.LogDebug("Sending read request for topic {0}", topic);

            try
            {
                //Convert the data to the required format
                var canFrame = Translator.TranslateBack(topic, null, CanSenderId, NoUnit, "1");

                await SendCanFrame(canServer, canPort, canFrame);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to send a read via CAN bus.");
            }
        }

        /// <summary>
        /// Sends a CAN frame to the CAN bus
        /// </summary>
        /// <param name="canServer">The CAN Server (where socketcand runs)</param>
        /// <param name="canPort">The CAN Server Port</param>
        /// <param name="canFrame">The actual frame to send</param>
        /// <returns></returns>
        private async Task SendCanFrame(string canServer, int canPort, string canFrame)
        {
            await ConnectTcpCanBus(canServer, canPort);

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
            Logger.LogInformation("Sending CAN Frame: {0}", canFrameSdCommand);
            TcpCanStream.Write(Encoding.Default.GetBytes(canFrameSdCommand));
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
                Logger.LogTrace("Already connected to SocketCanD.");
                return;
            }

            //Create TCP Client for connection to socketcand (=scd)
            while (ScdClient == null || !ScdClient.Connected)
            {
                try
                {
                    ScdClient = new TcpClient(canServer, canPort);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "FAILED TO CONNECT TO SOCKETCAND {1}. Retry...", canServer);
                }
                await Task.Delay(TimeSpan.FromSeconds(5));
            }

            Logger.LogInformation("CONNECTED TO SOCKETCAND {0} ON PORT {1}", canServer, canPort);

            //Create TCP Stream to read the CAN Bus Data
            byte[] data = new byte[CanReceiveBufferSize];
            TcpCanStream = ScdClient.GetStream();
            int bytes = TcpCanStream.Read(data, 0, data.Length);


            if (Encoding.Default.GetString(data, 0, bytes) == "< hi >")
            {
                Logger.LogInformation("Handshake successful. Opening CAN interface...");
                TcpCanStream.Write(Encoding.Default.GetBytes("< open " + CanInterfaceName + " >"));

                bytes = TcpCanStream.Read(data, 0, data.Length);
                if (Encoding.Default.GetString(data, 0, bytes) == "< ok >")
                {
                    Logger.LogInformation("Opening connection to slcan0 successful. Changing socketcand mode to raw...");
                    TcpCanStream.Write(Encoding.Default.GetBytes("< rawmode >"));

                    bytes = TcpCanStream.Read(data, 0, data.Length);
                    if (Encoding.Default.GetString(data, 0, bytes) == "< ok >")
                    {
                        Logger.LogInformation("Change to rawmode successful");
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
                            Logger.LogWarning("Dropping \"{0}\" because it is not expected at the beginning of a frame.", responseData.Substring(0, responseData.IndexOf("< frame ")));
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
                        Logger.LogWarning("No closing tag found. Save data and get next bytes.");
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

                        Logger.LogInformation("Received CAN Frame: {0}", canFrame.RawFrame);
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

                Logger.LogInformation("Disconnected from canServer {0} Port {1}", canServer, canPort);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error while reading CanBus Server.");
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
                if (Translator != null)
                {
                    canMsg = Translator.Translate(canMsg, NoUnit, Language, ConvertUnknown);
                }

                //Verify connection to MQTT Broker is established
                while (!MqttClient.IsConnected)
                {
                    Logger.LogInformation("UNHANDLED DISCONNECT FROM MQTT BROKER");
                    while (!MqttClient.IsConnected)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(5));

                        try
                        {
                            await MqttClient.ConnectAsync(MqttClientOptions);
                            Logger.LogInformation("CONNECTED TO MQTT BROKER");
                        }
                        catch
                        {
                            Logger.LogInformation("RECONNECTING TO MQTT BROKER FAILED. Retrying...");
                        }
                    }
                }

                if (string.IsNullOrEmpty(canMsg.MqttTopicExtention))
                {
                    return;
                }

                //Logoutput with or without translated MQTT message
                if (string.IsNullOrEmpty(canMsg.MqttValue))
                    Logger.LogInformation("Sending MQTT Message: {0} and Topic {1}", canMsg.PayloadFull.Trim(), MqttTopic);
                else
                    Logger.LogInformation("Sending MQTT Message: {0} and Topic {1}{2}", canMsg.MqttValue, MqttTopic, canMsg.MqttTopicExtention);

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
            catch (Exception ex)
            {
                Logger.LogError(ex, "ERROR while sending MQTT message.");
            }
        }

        private async Task SetupMqtt()
        {
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
                Logger.LogInformation("Connecting to MQTT broker using Credentials...");
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
                Logger.LogWarning("DISCONNECTED FROM MQTT BROKER {0} because of {1}", MqttServer, e.Reason);
                while (!MqttClient.IsConnected)
                {
                    try
                    {
                        // Connect the MQTT Client
                        await MqttClient.ConnectAsync(MqttClientOptions);
                        if (MqttClient.IsConnected)
                            Logger.LogInformation("CONNECTED TO MQTT BROKER {0} using ClientId {1}", MqttServer, MqttClientId);
                        else
                            Logger.LogInformation("CONNECTION TO MQTT BROKER {0} using ClientId {1} FAILED", MqttServer, MqttClientId);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogInformation("RECONNECTING TO MQTT BROKER {0} FAILED. Exception: {1}", MqttServer, ex.ToString());
                        Thread.Sleep(10000); //Wait 10 seconds
                    }
                }
            });

            // Connect the MQTT Client to the MQTT Broker
            await MqttClient.ConnectAsync(MqttClientOptions);
            if (MqttClient.IsConnected)
                Logger.LogInformation("CONNECTION TO MQTT BROKER {0} established using ClientId {1}", MqttServer, MqttClientId);

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
                            Logger.LogInformation($" and Payload = {Encoding.UTF8.GetString(e.ApplicationMessage.Payload)}");
                            await SendCan(e.ApplicationMessage.Topic, e.ApplicationMessage.Payload, CanServer, CanServerPort);
                        }
                        else
                        {
                            Logger.LogInformation(" WITH NO PAYLOAD");
                        }
                    }
                    // Check if it is a read topic. If yes, send a READ via CAN bus for the corresponding value to trigger a send of the value via CAN bus
                    else if (e.ApplicationMessage.Topic.EndsWith("/read"))
                    {
                        Console.Write("Received MQTT READ Message; Topic = {0}", e.ApplicationMessage.Topic);
                        if (e.ApplicationMessage.Topic != null)
                        {
                            Logger.LogInformation("");
                            await ReadCan(e.ApplicationMessage.Topic, CanServer, CanServerPort);
                        }
                        else
                        {
                            Logger.LogInformation(" WITH NO TOPIC");
                        }
                    }
                });
            }
        }

        void SetupAutoPolling(CancellationToken stoppingToken)
        {
            if (!AutoPolling)
            {
                return;
            }

            AutoPollingTask = Task.Run(async () =>
            {
                if (Translator == null)
                {
                    Logger.LogWarning("Nothing to poll - no translator selected.");
                    return;
                }

                if (!Translator.MqttTopicsToPoll.Any())
                {
                    Logger.LogWarning("Nothing to poll - no MQTT topics to poll specified (or all are ignored).");
                    return;
                }

                var delay = TimeSpan.FromSeconds(AutoPollingInterval);
                while (!stoppingToken.IsCancellationRequested)
                {
                    await Task.Delay(delay, stoppingToken);

                    foreach (var mqttTopic in Translator.MqttTopicsToPoll)
                    {
                        await ReadCan(mqttTopic + "/read", CanServer, CanServerPort);
                        // delay next read to avoid overloading socketcand
                        await Task.Delay(AutoPollingThrottle, stoppingToken);
                    }
                }
            });
        }
    }
}
