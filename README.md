# can2mqtt
A Linux or Windows service to forward CAN frames to MQTT messages
The precompiled version you can find in the releases section may not be the latest version described below. Check the version number for differences. To get the very latest version, build from code.

# Latest Updates

## V5.0

Breaking changes:
 - Running on .NET 8.0 instead of .NET 6.0

New features:
 - Added option `ConvertUnknown`, this defines if values of an unknown typed message (e.g., no entry in StiebelEltron.json) shall be converted with all possible formats and printed to console.
 - Added fallback value converter for unknown indexes in StiebelEltron.json for easier debugging.

Fixes:
 - Do not publish unknown values to MQTT broker
 - Load StiebelEltron.json only once during runtime

## V4.4
This software was just updated. The main and breaking changes are:
- Running on .NET 6.0 instead of Dotnet Core 2.2
- Using SOCKETCAND instead of CANLOGSERVER
- MQTT Topics changed
- Supporting MQTT Sends
- Supporting authentication on MQTT broker
- Moved config to json file instead of parameters

# HowTo
Note: This whole readme assumes the following environment:
- You are running and Ubuntu based Linux distribution on a Raspberry Pi (Tested with Raspberry Pi OS and openHABian)
- You are using the user "Pi" (if you have something else, just replace the username wherever stated)
- You are using a fresh installation of the OS
- You are using a USBtin device to connect to the CAN Bus (others might work but are not tested by me)
- In thoery (and also during development I did this), socketcand and canutils can run the Raspberry Pi while can2mqtt is running on a another device in the network. That way you can also use a Raspberry Pi Zero (1st Gen) for socketcand and can-utils while running can2mqtt on another system that supports the Dotnet Framework (requires ARMv7 or later architecture).


# Installation and Setup
## Install .NET 6.0 Runtime
Note, the packagelink may be differnt if you are not using a OS based on ubuntu 21.04 or 21.10. Check this page for other releases: https://docs.microsoft.com/en-us/dotnet/core/install/linux-ubuntu
This are the links from https://dotnet.microsoft.com/en-us/download/dotnet/6.0 for the differente Processor architectures for Linux:
ARM32: https://download.visualstudio.microsoft.com/download/pr/f8e1ab66-58f7-4ebb-a9bb-9decfa03501f/88e1fb49af6f75dc54c23383162409c5/dotnet-runtime-6.0.4-linux-arm.tar.gz
ARM64: https://download.visualstudio.microsoft.com/download/pr/3641affa-8bb0-486f-93d9-68adff4f4af7/1e3df9fb86cba7299b9e575233975734/dotnet-runtime-6.0.4-linux-arm64.tar.gz
x64: https://download.visualstudio.microsoft.com/download/pr/5b08d331-15ac-4a53-82a5-522fa45b1b99/65ae300dd160ae0b88b91dd78834ce3e/dotnet-runtime-6.0.4-linux-x64.tar.gz
Use the link you need for your installation in the command below. Below I will uses ARM32.
```
wget https://download.visualstudio.microsoft.com/download/pr/f8e1ab66-58f7-4ebb-a9bb-9decfa03501f/88e1fb49af6f75dc54c23383162409c5/dotnet-runtime-6.0.4-linux-arm.tar.gz -O ~/dotnet6.0.4.tar.gz
sudo tar -xvf dotnet6.0.4.tar.gz -C /opt/dotnet/
sudo ln -s /opt/dotnet/dotnet /usr/local/bin
```

Check the setup by run "dotnet --info". It should return the installed version and some other details.

## can-utils installation and setup
### Install can-utils
```
sudo apt-get install git
sudo mkdir /opt
cd /opt
git clone https://github.com/linux-can/can-utils.git
cd can-utils
make
```

### Setup the CAN Bus connection 
Assuming your device has the ID ttyACM0:
```
sudo /opt/can-utils/slcan_attach -f -s1 -b 11 -o /dev/ttyACM0
sudo /opt/can-utils/slcand ttyACM0 slcan0
sudo ifconfig slcan0 up
```

To add this to autostart and setup the adapter on every reboot, run 'sudo nano /etc/rc.local'
Paste the three lines above at the end before the "EXIT 0".

## socketcand installation and setup
### Install socketcand
```
sudo apt-get install git autoconf
cd ~
git clone https://github.com/linux-can/socketcand.git
cd socketcand
./autogen.sh
./configure
make
make install
sudo mv ~/socketcand /opt
```

### Start socketcand
This is only required to test and debug or if you like to take care of socketcand on your own.
You need to replace eth0 if your network interface is called different than eth0.
```
 /opt/socketcand/socketcand -i slcan0 -l eth0 -v
```

### Setup socketcand as daemon
Execute 'sudo nano /etc/systemd/system/socketcand.service', replace the network interface name if it is not eth0, replace the user with a username the daemon will use to run and paste the following:
```
[Unit]
Description=socketcand
After=network.target

[Service]
ExecStart=/opt/socketcand/socketcand -i slcan0 -l eth0 -v
WorkingDirectory=/opt/socketcand/
StandardOutput=inherit
StandardError=inherit
Restart=always
User=pi

[Install]
WantedBy=multi-user.target
```

Finally run the following commands:
```
sudo systemctl enable socketcand
sudo systemctl start socketcand
```

## MQTT Broker
You need an MQTT Broker, that is handling the MQTT traffic. You need to define the MQTT Broker IP in the can2mqtt config file later.
If you don't have any MQTT broker, Mosquitto is a common MQTT Broker. Please check out on yourself how to install and configure it.

## can2mqtt
### Download can2mqtt
```
cd ~
wget https://github.com/Hunv/can2mqtt/releases/download/v4/can2mqtt_v4.zip.zip -O can2mqtt.zip
sudo unzip can2mqtt.zip -d /opt/can2mqtt
```

### Start can2mqtt: 
Optional: This is only required to test and debug or if you like to take care of can2mqtt on your own by running can2mqtt interactivly. 
You can also configure a daemon to do this automatically (see below).
```
dotnet /opt/can2mqtt/can2mqtt.dll
```

### Setup can2mqtt as daemon
Execute 'sudo nano /etc/systemd/system/can2mqtt.service', replace the user with a username the daemon will use to run and paste the following:
```
[Unit]
Description=can2mqtt
After=network.target

[Service]
ExecStart=dotnet /opt/can2mqtt/can2mqtt.dll
WorkingDirectory=/opt/can2mqtt/
StandardOutput=inherit
StandardError=inherit
Restart=always
User=pi

[Install]
WantedBy=multi-user.target
```

### Configure config.json:
First copy the sample config.json file: 'sudo cp /opt/can2mqtt/config-sample.json /opt/can2mqtt/config.json'
Edit the config.json with your favorite editor (i.e. nano): 'sudo nano /opt/can2mqtt/config.json'
```
{
  "CanServer": "192.168.0.10",      < This is the System where socketcand is running on
  "CanServerPort": 29536,           < This is the port socketcand is using (29536 is default)
  "CanForwardWrite": true,          < This defines if can2mqtt will handle CAN bus packages, that have the "write" flag
  "CanForwardRead": true,           < This defines if can2mqtt will handle CAN bus packages, that have the "read" flag
  "CanForwardResponse": true,       < This defines if can2mqtt will handle CAN bus packages, that have the "response" flag
  "CanReceiveBufferSize": 48,       < The buffer size of receiving commands. 48 is default.
  "CanSenderId":"6A2",              < The ID can2mqtt will use at the CAN bus in case of writing to the CAN bus
  "CanInterfaceName":"slcan0",      < The Interface name to use for the CAN bus connection

  "MqttServer": "192.168.0.10",     < This is the IP of the MQTT Broker
  "MqttClientId": "Can2Mqtt",       < This is the ID the MQTT Client will use when register at MQTT Broker
  "MqttTopic": "Heating",           < This is the first path item of the MQTT topic path can2mqtt will use for send/receive information
  "MqttTranslator": "StiebelEltron",< This is the translator used to translate the CAN bus data to values and send it via MQTT
  "MqttUser": "",                   < This is the user that is required to register at the MQTT broker. Leave empty for none.
  "MqttPassword": "",               < This is the password that is required to register at the MQTT broker. Leave empty for none.
  "MqttAcceptSet": false,           < This is a setting, that defines if can2mqtt will send write-commands to the CAN bus. For safety reasons the default setting is set to false.  

  "NoUnits": true,                  < This defines if sending MQTT messages will contain the unit defined in the translator config or not (i.e. "25°C" or just "25")  
  "Language": "en",                 < This defines the language, that will be used. Currently available languages are "en" (English) and "de" (German).

  "ConvertUnknown": false           < New in Release 5.0; This defines if values of an unknown typed message (e.g., no entry in StiebelEltron.json) shall be converted with all possible formats and printed to console.
}
```

Finally run the following commands to start the daemon:
```
sudo systemctl enable can2mqtt
sudo systemctl start can2mqtt
```

# MQTT Data format
can2mqtt sends the data in the topics, that are defined in the config of the translator. In case of the Stiebel Eltron translator it is the 'StiebelEltron.json'. There will be a prefix-topic in case there are other instances of can2mqtt or other MQTT applications. This one is configured in the config.json at the setting 'MqttTopic'.
An example MQTT message may look like this:
Topic: heating/outside/temperature/measured
Value: 21°C

If you like to set data, add a /set at the end of the topic.
An example MQTT message to can2mqtt to set the desired room temperature of the primary heat cycle to 23°C:
Topic: heating/room/hc1/temperature/day/set
Value: 23 (Important: Without the unit!)

If you need to request a send from the CAN bus, you can add a /read at the end of the topic.
An example MQTT message to can2mqtt to request the value of the desired room temperature of the primary heat cycle:
Topic: heating/room/hc1/temperature/day/read


# Troubleshooting
## Issue: The connection is reconnecting over and over again multiple times within seconds until the application crashes
Reason: Do you have a second MQTT client registered on the MQTT Broker with the same client ID?
