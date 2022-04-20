# can2mqtt
A Linux or Windows service to forward CAN frames to MQTT messages

# Latest Updates
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

## Install socketcand
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

## Install can-utils
```
sudo apt-get install git
cd ~
git clone https://github.com/linux-can/can-utils.git
cd can-utils
make
```

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

## Setup the CAN Bus connection 
Assuming your device has the ID ttyACM0:
```
sudo /opt/can-utils/slcan_attach -f -s1 -b 11 -o /dev/ttyACM0
sudo /opt/can-utils/slcand ttyACM0 slcan0
sudo ifconfig slcan0 up
```

To add this to autostart and setup the adapter on every reboot, run "sudo nano /etc/rc.local"
paste three lines above at the end before the "EXIT 0".

## Start socketcand
This is only required to test or if you like to take care of socketcand on your own. You can also configure a daemon to do this automatically (see below).
You need to replace eth0 if your network interface is called different than eth0.
```
 /opt/socketcand/socketcand -i slcan0 -l eth0 -v
```

## MQTT Broker
You need an MQTT Broker, that is handling the MQTT Traffic. You need to define this in the settings.

## Start can2mqtt: 
This is only required to test or if you like to take care of can2mqtt on your own. You can also configure a daemon to do this automatically (see below).

Minimum parameter: `./can2mqtt_core --Daemon:MqttServer="192.168.0.192"`

All parameter: `./can2mqtt_core --Daemon:Name="Can2MqttSE" --Daemon:CanServer="192.168.0.192" --Daemon:CanServerPort=28700 --Daemon:MqttServer="192.168.0.192" --Daemon:MqttClientId="Can2Mqtt" --Daemon:MqttTopic="Heating" --Daemon:MqttTranslator="StiebelEltron" --Daemon:CanForwardWrite=true --Daemon:CanForwardRead=false --Daemon:CanForwardResponse=true`

### Startup Parameters:

| Parameter                     | Description                                                                                                                                                                                                                                                               | Default Value | Required | Example                                   |
|-------------------------------|---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|---------------|----------|-------------------------------------------|
| `--Daemon:Name`               | Define the name of your Daemon                                                                                                                                                                                                                                            | Can2Mqtt      | No       | `--Daemon:Name="Can2MqttSE"`              |
| `--Daemon:CanServer`          | This is the address where your canlogserver is running                                                                                                                                                                                                                    | 127.0.0.1     | No       | `--Daemon:CanServer="192.168.0.192"`      |
| `--Daemon:CanServerPort`      | This is the port of the canlogserver                                                                                                                                                                                                                                      | 28700         | No       | `--Daemon:CanServerPort=28700`            |
| `--Daemon:MqttServer`         | This is the address of the MQTT Broker.                                                                                                                                                                                                                                   |               | Yes      | `--Daemon:MqttServer="192.168.0.192"`     |
| `--Daemon:MqttClientId`       | This is the clientId of the MQTT Client. Choose any name you like                                                                                                                                                                                                         | Can2Mqtt      | No       | `--Daemon:MqttClientId="Can2Mqtt"`        |
| `--Daemon:MqttTopic`          | This is the MQTT Root Topic of all MQTT message                                                                                                                                                                                                                           | Can2Mqtt      | No       | `--Daemon:MqttTopic="Heating"`            |
| `--Daemon:MqttTranslator`     | For some CAN Bus Clients are translators to translate the CAN Messages into a readable value and publish them via MQTT including the correct topic. Leave empty to publish every CAN frame without any further handling. Implemented translators right now: StiebelEltron |               | No       | `--Daemon:MqttTranslator="StiebelEltron"` |
| `--Daemon:CanForwardWrite`    | Should CAN frames of type "Write" be forwarded to MQTT?                                                                                                                                                                                                                   | true          | No       | `--Daemon:CanForwardWrite=true`           |
| `--Daemon:CanForwardRead`     | Should CAN frames of type "Read" be forwarded to MQTT?                                                                                                                                                                                                                    | false         | No       | `--Daemon:CanForwardRead=false`           |
| `--Daemon:CanForwardResponse` | Should CAN frames of type "Response" be forwarded to MQTT?                                                                                                                                                                                                                | true          | No       | `--Daemon:CanForwardResponse=true`        |


## Configure and Register Daemon for can2mqtt and canlogserver
Execute `sudo nano /etc/systemd/system/canlogserver.service` and paste the following into the file. Replace the slcan0 in case your socket has a different name:
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

Now we repeat this for the can2mqtt daemon. Execute `sudo nano /etc/systemd/system/can2mqtt.service` and paste the following into the file. Replace the placeholder with your parameters:
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