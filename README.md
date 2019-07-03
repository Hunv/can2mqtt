# can2mqtt
A Linux or Windows service to forward CAN frames to MQTT messages

# HowTo
Note: This whole readme assumes the following environment:
- You are running Rasbian on a Raspberry Pi
- You are using the user "Pi"
- You are using a fresh installation of the OS

## Install can-utils
```
sudo apt-get install git
cd ~
git clone https://github.com/linux-can/can-utils.git
cd can-utils
make
```

## Install Dotnet Core
```
wget https://download.visualstudio.microsoft.com/download/pr/87521bd8-1522-4141-9532-91d580292c42/59116d9f6ebced4fdc8b76b9e3bbabbf/dotnet-runtime-2.2.5-linux-arm.tar.gz
sudo tar -xvf ./dotnet-runtime-2.2.5-linux-arm.tar.gz -C /opt/dotnet/
sudo ln -s /opt/dotnet/dotnet /usr/local/bin
```

## Setup the CAN Bus connection 
Assuming your device has the ID ttyACM0:
```
sudo ./slcan_attach -f -s1 -b 11 -o /dev/ttyACM0
sudo ./slcand ttyACM0 slcan0
sudo ifconfig slcan0 up
```

## Start canlogserver
This is only required, if you like to take care of canlogserver on your own. can2mqtt is able to start the canlogserver automatically. Use parameters `--Daemon:CanlogserverPath` and `--Daemon:CanlogserverSocket` to use that option.
```
~/can-utils/canlogserver slcan0
```

## Start can2mqtt: 
Minimum parameter: `./can2mqtt_core --Daemon:MqttServer="192.168.0.192"`

All parameter: `./can2mqtt_core --Daemon:Name="Can2MqttSE" --Daemon:CanServer="192.168.0.192" --Daemon:CanServerPort=28700 --Daemon:MqttServer="192.168.0.192" --Daemon:MqttClientId="Can2Mqtt" --Daemon:MqttTopic="Heating" --Daemon:MqttTranslator="StiebelEltron" --Daemon:CanForwardWrite=true --Daemon:CanForwardRead=false --Daemon:CanForwardResponse=true

### Startup Parameters:
`--Daemon:Name="Can2MqttSE"`

Define the name of your Daemon. Default: Can2Mqtt


`--Daemon:CanServer="192.168.0.192" `

This is the address where your canlogserver is running. Default: 127.0.0.1


`--Daemon:CanServerPort=28700 `

This is the port of the canlogserver. Default: 28700


`--Daemon:MqttServer="192.168.0.192" `

This is the address of the MQTT Broker.


`--Daemon:MqttClientId="Can2Mqtt" `

This is the clientId of the MQTT Client. Choose any name you like. Default: Can2Mqtt


`--Daemon:MqttTopic="Heating" `

This is the MQTT Root Topic of all MQTT message. Default: Can2Mqtt


`--Daemon:MqttTranslator="StiebelEltron"`

For some CAN Bus Clients are translators to translate the CAN Messages into a readable value and publish them via MQTT including the correct topic. Leave empty to publish every CAN frame without any further handling.
Implemented translators right now: StiebelEltron


`--Daemon:CanForwardWrite=true`

Should CAN frames of type "Write" be forwarded to MQTT? Default: true


`--Daemon:CanForwardRead=false`

Should CAN frames of type "Read" be forwarded to MQTT? Default: false


`--Daemon:CanForwardResponse=true`

Should CAN frames of type "Response" be forwarded to MQTT? Default: true


## Register to start canlogserver and can2mqtt on startup
Execute `sudo nano etc/systemd/system/canlogserver.service` and paste the following into the file. Replace the slcan0 in case your socket has a different name:
```
[Unit]
Description=canlogserver
After=network.target

[Service]
ExecStart=/home/pi/can-utils/canlogserver slcan0
WorkingDirectory=/home/pi/can-utils/canlogserver
StandardOutput=inherit
StandardError=inherit
Restart=always
User=pi

[Install]
WantedBy=multi-user.target
```
Run `sudo systemctl start canlogserver.service` to test if the service starts (it should). To see if it runs, execute `sudo systemctl status canlogserver.service`. You should see something similar like this if everything went well in the last line: `Jul 03 19:09:16 raspi-test systemd[1]: Started canlogserver.`

Now we repeat this for the can2mqtt daemon. Execute `sudo nano etc/systemd/system/can2mqtt.service` and paste the following into the file. Replace the placeholder with your parameters:
```
[Unit]
Description=can2mqtt
After=network.target

[Service]
ExecStart=/usr/local/bin/dotnet /home/pi/can2mqtt_core/can2mqtt_core.dll >>>STATE YOUR PARAMETERS HERE<<<
WorkingDirectory=/home/pi/can2mqtt_core/
StandardOutput=inherit
StandardError=inherit
Restart=always
User=pi

[Install]
WantedBy=multi-user.target
```
An example for the ExecStart line is:
```ExecStart=/usr/local/bin/dotnet /home/pi/can2mqtt_core/can2mqtt_core.dll --Daemon:MqttServer="127.0.0.1" --Daemon:MqttClientId="Can2Mqtt" --Daemon:MqttTopic="Heating" --Daemon:MqttTranslator="StiebelEltron" --Daemon:CanlogserverPath="/home/pi/can-utils" --Daemon:CanlogserverSocket="slcan0"```

To test if it works, run the following command: `sudo systemctl start can2mqtt.service`. To test if the service is running, execute `sudo systemctl status can2mqtt.service`. You will see the last lines of output. If you do not see any errors after about 10 seconds, everything is fine.

Finally if everything is fine and you want to autostart the canlogserver and can2mqtt application, execute this: 
```
sudo systemctl enable canlogserver.service
sudo systemctl enable can2mqtt.service
```