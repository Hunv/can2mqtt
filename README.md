# can2mqtt
A Linux or Windows service to forward CAN frames to MQTT messages

# HowTo

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
```
~/can-utils/canlogserver slcan0
```

## Start can2mqtt: 
Minimum parameter: `./can2mqtt_core --Daemon:MqttServer="192.168.0.192"`
All parameter: `./can2mqtt_core --Daemon:Name="Can2MqttSE" --Daemon:CanServer="192.168.0.192" --Daemon:CanServerPort=28700 --Daemon:MqttServer="192.168.0.192" --Daemon:MqttClientId="Can2Mqtt" --Daemon:MqttTopic="Heating" --Daemon:MqttTranslator="StiebelEltron" --Daemon:CanForwardWrite=true --Daemon:CanForwardRead=false --Daemon:CanForwardResponse=true`

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