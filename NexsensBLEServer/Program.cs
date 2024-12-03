using System;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.Sockets;
using System.Text;
using InTheHand.Bluetooth;
//using InTheHand.Net.Bluetooth;
using Microsoft.Extensions.Logging;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Enumeration;
using Windows.Media.Streaming.Adaptive;


//using Microsoft.Extensions.Logging;


namespace Server
{

    class Program
    {
        static DeviceInformationKind deviceInformationKind;
        static DeviceInformation rtudevice;

        static IReadOnlyCollection<BluetoothDevice> _devices;
        static List<GattService> _services;
        int read_index = -1;
        int write_index = -1;
        static ILogger logger;
        static string custom_data = "a3ab6eae-9eb9-40a7-be2a-038312f3313a";
        static string custom_data_read = "eb7a0696-4866-46fb-8513-ffd5bcf596fd";
        static string custom_data_write = "21a65022-e96d-4961-869a-82e78b334e59";

    static void Main(string[] args)
        {
            string logFilePath = "console_log.txt";

            using ILoggerFactory factory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
                //builder.AddProvider(new CustomFileLoggerProvider(logFileWriter));
                //https://github.com/dotnet/samples/tree/main/core/logging
                //https://learn.microsoft.com/en-us/answers/questions/1377949/logging-in-c-to-a-text-file
            });
            logger = factory.CreateLogger("Nexsens");
            //LogStartupMessage(logger, "fun");
            logger.LogInformation("Welcome to NexSens BLE Socket Server");
            ExecuteServer();
        }

        public static async Task WriteCustom(Socket client,string data)
        {
            await Task.Run(() => WriteCustomAsync(client,data));
        }
        public static async Task WriteCustomAsync(Socket client, string data)
        {
            byte[] mbaddr = { 0x01,0x03,0x10,0x00,0x00,0x01,0x80,0xca};
            byte[] fwver = { 0x01, 0x03, 0x10, 0x09, 0x00, 0x02, 0x10, 0xc9 };
            byte[] devid = { 0x01,0x03,0x10,0x07,0x00,0x02,0x71,0x0a };
            try
            {
                logger.LogInformation("Attempting to write now...");
                var read_ser = _services.Find(x => x.Uuid.ToString() == custom_data);
                if (read_ser != null)
                {
                    var characteristics = await read_ser.GetCharacteristicsAsync();
                    if (characteristics != null)
                    {
                        byte[] value;
                        foreach (var characteristic in characteristics)
                        {
                            if (characteristic.Uuid.ToString() == custom_data_write)
                            {
                                if (data == "devid") //devid
                                {
                                    value = devid;
                                }
                                else if (data == "mbaddr")
                                {
                                    value = mbaddr;
                                }
                                else if (data == "fwver")//firmware version
                                {
                                    value = fwver;
                                }
                                else
                                {
                                    logger.LogInformation("No valid input given to write");
                                    return;
                                }
                                logger.LogInformation("writing value " + BitConverter.ToString(value));
                                await characteristic.WriteValueWithoutResponseAsync(value);
                            }

                        }

                    }
                }
                else
                {
                    logger.LogInformation("Did not find service with custom_read_data UUID");
                }

            }
            catch (Exception ex)
            {
                logger.LogInformation(ex.StackTrace);
            }
        }
        public static async Task ReadCustom(Socket client)
        {
            await Task.Run(() => ReadCustomAsync(client));
        }
        public static async Task ReadCustomAsync(Socket client)
        {
            try
            {
                logger.LogInformation("Attempting to read now...");
                var read_ser = _services.Find(x => x.Uuid.ToString() == custom_data);
                if (read_ser != null)
                {
                    var characteristics = await read_ser.GetCharacteristicsAsync();
                    if (characteristics != null)
                    {
                        string value = string.Empty;
                        
                            foreach (var characteristic in characteristics)
                        {
                            if(characteristic.Uuid.ToString() == custom_data_read)
                            {
                                var rawValue = await characteristic.ReadValueAsync();                             
                                string stringByte = BitConverter.ToString(rawValue);
                                logger.LogInformation("Read value on custom_data_read characteristic is "+stringByte);
                                client.Send(Encoding.ASCII.GetBytes(stringByte));
                            }

                        }

                    }
                }
                else
                {
                    logger.LogInformation("Did not find service with custom_read_data UUID");
                }
               
            }
            catch (Exception ex)
            {
                logger.LogInformation(ex.StackTrace);
            }
        }
        public static  async Task ConnectDevice(int index)

        {
            await Task.Run(() => ConnectDeviceAsync(index));
        }
        private static void handlerPairingReq(DeviceInformationCustomPairing CP, DevicePairingRequestedEventArgs DPR)
        {
            //so we get here for custom pairing request.
            //this is the magic place where your pin goes.
            //my device actually does not require a pin but
            //windows requires at least a "0".  So this solved 
            //it.  This does not pull up the Windows UI either.
            logger.LogInformation("Handling pairing req");
            DPR.Accept("999888");
        }

        public static async Task ConnectDeviceAsync(int index)
        {
            DeviceInformationCollection deviceInfoCollection;

            try
            {
                logger.LogInformation("Connecting to "+_devices.ElementAt(index).Name);
                //BluetoothDevice rtuDevice = _devices.Single(d => d.Name == "X3RTU");
                
                var gatt = _devices.ElementAt(index).Gatt;

                deviceInfoCollection = await DeviceInformation.FindAllAsync(Windows.Devices.Bluetooth.BluetoothLEDevice.GetDeviceSelectorFromPairingState(false));
                if (deviceInfoCollection.Count > 0)
                {
                    // When you want to "save" a DeviceInformation to get it back again later,
                    // use both the DeviceInformation.Kind and the DeviceInformation.Id.
                    //interfaceIdTextBox.Text = deviceInfoCollection[0].Id;
                    //deviceInformationKind = deviceInfoCollection[0].Kind;
                    //InformationKindTextBox.Text = deviceInformationKind.ToString();
                    //getButton.IsEnabled = true;
                    foreach (var d in deviceInfoCollection)
                    {
                        logger.LogInformation(d.Name + " || " + d.Id + " || " + d.Kind.ToString());
                        if (d.Name.Contains("RTU"))
                        {
                            logger.LogInformation("Found RTU device");
                            //DevicePairingResult dpr = await d.Pairing.PairAsync();
                            //logger.LogInformation("Pairing status " + dpr.Status.ToString());
                            rtudevice = d;
                        }
                    }

                }
                else
                {
                    logger.LogInformation("No devices in collection");
                }

                await gatt.ConnectAsync();
                if (gatt.IsConnected)
                {
                    logger.LogInformation("Connected to gatt server");
                    if(rtudevice != null)
                    {
                        rtudevice.Pairing.Custom.PairingRequested += handlerPairingReq;

                        DevicePairingResult dpr = await rtudevice.Pairing.Custom.PairAsync(DevicePairingKinds.ProvidePin, DevicePairingProtectionLevel.EncryptionAndAuthentication);
                        //.PairAsync(DevicePairingProtectionLevel.EncryptionAndAuthentication);
                        logger.LogInformation("Pairing status " + dpr.Status.ToString());

                    }


                    _services = await gatt.GetPrimaryServicesAsync();
                    foreach (var service in _services)
                    {
                        var serviceName = GattServiceUuids.GetServiceName(service.Uuid);
                        if (string.IsNullOrWhiteSpace(serviceName))
                            serviceName = service.Uuid.ToString();
                        logger.LogInformation("Service is "+serviceName);
                        var characteristics = await service.GetCharacteristicsAsync();
                        foreach (var characteristic in characteristics)
                        {
                            var charName = GattCharacteristicUuids.GetCharacteristicName(characteristic.Uuid);
                            string value = string.Empty;

                            if (string.IsNullOrWhiteSpace(charName))
                                charName = characteristic.Uuid.ToString();

                            logger.LogInformation("Char name is "+charName);
                            if (characteristic.Uuid.ToString() is not null)
                            {
                                
                                var rawValue = await characteristic.ReadValueAsync();
                                if (rawValue != null)
                                {
                                    value = System.Text.Encoding.UTF8.GetString(rawValue).Trim();
                                    logger.LogInformation("Read value is " + value);
                                }
                                else
                                {
                                    logger.LogInformation("Value read as NULL!!");
                                    //DeviceInformation d = new DeviceInformation(characteristic.Uuid);
                                    //DevicePairingResult result = await DeviceInfo.Pairing.PairAsync();

                                }

                            }

                        }

                    }
                }

            }
            catch (Exception e)
            {
                logger.LogInformation(e.StackTrace);
            }
        }
        public static async Task SearchDevices(Socket client)
        {
            await Task.Run(()=>SearchDevicesAsync(client));
        }
        public static async Task SearchDevicesAsync(Socket client)
        {
            try
            {


                
                //return;
                _devices = await Bluetooth.ScanForDevicesAsync();
                logger.LogInformation($"found {_devices?.Count} devices");
                int i = 0;
                string send_str = "";
                foreach (var d in _devices)
                {
                    logger.LogInformation(i +" | Name:"+d.Name+" | ID"+d.Id + " | IsPaired"+d.IsPaired);
                    send_str = send_str + ";" + i + "," + d.Name;
                    i++;
                }
                
                client.Send(Encoding.ASCII.GetBytes(send_str));

                //BluetoothDevice rtuDevice = _devices.Single(d => d.Name == "X3RTU");
                //var gatt = rtuDevice.Gatt;
                //await gatt.ConnectAsync();
                //BluetoothUuid nameService = new BluetoothUuid("2A29");


                //GattService rtuService = await gatt.GetPrimaryServiceAsync();

                //GattCharacteristic fooChar = await rtuService.GetCharacteristicAsync(Guid.Parse(BleUuids.FOO_CHAR));

            }
            catch (Exception e)
            {
                logger.LogInformation("In exception",e.Message);
            }

        }

        public static void ScanBLE()
        {
            logger.LogInformation("Scanning nearby bluetooth LE devices...");
        }
        
        public static int Command_Handler(string command, Socket client)
        {
            if (command == "stop")
                return -1;
            
            else if (command.Contains("connect"))
            {
                logger.LogInformation("Command is "+command);
                string v = command.Split(" ")[1];
                int index = Int32.Parse(v);
                //logger.LogInformation("Connecting to device "+_devices.ElementAt(index).Name);
                ConnectDevice(index);
            }
            else if (command == "scan")
            {
                logger.LogInformation("Scanning..");

                SearchDevices(client);
                //Task.Delay(10000);
            }
            else if(command == "read")
            {
                ReadCustom(client);
            }
            else if (command.Contains("write"))
            {
                string v = command.Split(" ")[1];
                logger.LogInformation(v);
                WriteCustom(client, v);
            }
            return 0;
        }
        public static void ExecuteServer()
        {
            // Establish the local endpoint 
            // for the socket. Dns.GetHostName
            // returns the name of the host 
            // running the application.

            IPHostEntry ipHost = Dns.GetHostEntry("localhost");
            IPAddress ipAddr = ipHost.AddressList[1];
            //IPAddress ipAddr = IPAddress.Parse("127.0.0.1");
            IPEndPoint localEndPoint = new IPEndPoint(ipAddr, 31003);

            // Creation TCP/IP Socket using 
            // Socket Class Constructor
            Socket listener = new Socket(ipAddr.AddressFamily,
                         SocketType.Stream, ProtocolType.Tcp);

            try
            {

                // Using Bind() method we associate a
                // network address to the Server Socket
                // All client that will connect to this 
                // Server Socket must know this network
                // Address
                listener.Bind(localEndPoint);

                // Using Listen() method we create 
                // the Client list that will want
                // to connect to Server
                listener.Listen(10);
                int ret;
                while (true)
                {

                    logger.LogInformation("Waiting connection on "+ipAddr.ToString());

                    // Suspend while waiting for
                    // incoming connection Using 
                    // Accept() method the server 
                    // will accept connection of client
                    Socket clientSocket = listener.Accept();
                    logger.LogInformation("Connected over socket!");

                    // Data buffer
                    byte[] bytes = new Byte[1024];
                    string data = null;
                    while (true)
                    {
                        while (true)
                        {

                            int numByte = clientSocket.Receive(bytes);

                            data += Encoding.ASCII.GetString(bytes,
                                                       0, numByte);
                            if (data.IndexOf("<EOM>") > -1)
                                break;
                        }
                        data = data.Split("<")[0];
                        logger.LogInformation("Command received -> {0} ", data);
                        byte[] message = Encoding.ASCII.GetBytes(data+"_ack");
                        clientSocket.Send(message);
                        ret = Command_Handler(data,clientSocket);
                        if (ret==-1)
                                break;
                        data = "";

                    }
                    // Close client Socket using the
                    // Close() method. After closing,
                    // we can use the closed Socket 
                    // for a new Client Connection
                    clientSocket.Shutdown(SocketShutdown.Both);
                    clientSocket.Close();
                    if (ret == -1)
                        break;
                }
            }

            catch (Exception e)
            {
                logger.LogInformation(e.ToString());
            }
        }
    }
}