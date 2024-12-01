using System;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.Sockets;
using System.Text;
using InTheHand.Bluetooth;
using InTheHand.Net.Bluetooth;
using Microsoft.Extensions.Logging;
using Windows.Devices.Enumeration;


//using Microsoft.Extensions.Logging;


namespace Server
{

    class Program
    {
 
        static IReadOnlyCollection<BluetoothDevice> _devices;
        static ILogger logger;
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

        public static  async Task ConnectDevice(int index)

        {
            await Task.Run(() => ConnectDeviceAsync(index));
        }
        public static async Task ConnectDeviceAsync(int index)
        {
            try
            {
                logger.LogInformation("Connecting to "+_devices.ElementAt(index).Name);
                //BluetoothDevice rtuDevice = _devices.Single(d => d.Name == "X3RTU");
                var gatt = _devices.ElementAt(index).Gatt;
                await gatt.ConnectAsync();
                if (gatt.IsConnected)
                {
                    var services = await gatt.GetPrimaryServicesAsync();
                    foreach (var service in services)
                    {
                        var serviceName = GattServiceUuids.GetServiceName(service.Uuid);
                        if (string.IsNullOrWhiteSpace(serviceName))
                            serviceName = service.Uuid.ToString();
                        logger.LogInformation(serviceName);
                        var characteristics = await service.GetCharacteristicsAsync();
                        foreach (var characteristic in characteristics)
                        {
                            var charName = GattCharacteristicUuids.GetCharacteristicName(characteristic.Uuid);
                            string value = string.Empty;

                            if (string.IsNullOrWhiteSpace(charName))
                                charName = characteristic.Uuid.ToString();
                            logger.LogInformation("Char name is "+charName);
                            if (characteristic.Uuid.ToString() is "2A24")
                            {
                                var rawValue = await characteristic.ReadValueAsync();
                                if (rawValue != null)
                                {
                                    value = System.Text.Encoding.UTF8.GetString(rawValue).Trim();
                                    logger.LogInformation("Read value is " + value);
                                }
                                else
                                {
                                    logger.LogInformation("Value read as NULL - check pairing");
                                    DeviceInformation d = new DeviceInformation(characteristic.Uuid);
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