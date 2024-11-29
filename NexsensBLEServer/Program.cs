using System;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.Sockets;
using System.Text;
using InTheHand.Bluetooth;


namespace Server
{
    class Program
    {       
        static IReadOnlyCollection<BluetoothDevice> _devices;
        static void Main(string[] args)
        {
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
                Console.WriteLine("Connecting to "+_devices.ElementAt(index).Name);
                //BluetoothDevice rtuDevice = _devices.Single(d => d.Name == "X3RTU");
                var gatt = _devices.ElementAt(index).Gatt;
                await gatt.ConnectAsync();

            }
            catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
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
                Console.WriteLine($"found {_devices?.Count} devices");
                int i = 0;
                string send_str = "";
                foreach (var d in _devices)
                {
                    Console.WriteLine(i +" | Name:"+d.Name+" | ID"+d.Id + " | IsPaired"+d.IsPaired);
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
                Console.WriteLine("In exception",e.Message);
            }

        }

        public static void ScanBLE()
        {
            Console.WriteLine("Scanning nearby bluetooth LE devices...");
        }
        
        public static int Command_Handler(string command, Socket client)
        {
            if (command == "stop")
                return -1;
            
            else if (command.Contains("connect"))
            {
                Console.WriteLine("Command is "+command);
                string v = command.Split(" ")[1];
                int index = Int32.Parse(v);
                Console.WriteLine("Connecting to device "+_devices.ElementAt(index).Name);
            }
            else if (command == "scan")
            {
                Console.WriteLine("Scanning..");

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

                    Console.WriteLine("Waiting connection on "+ipAddr.ToString());

                    // Suspend while waiting for
                    // incoming connection Using 
                    // Accept() method the server 
                    // will accept connection of client
                    Socket clientSocket = listener.Accept();
                    Console.WriteLine("Connected over socket!");

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
                        Console.WriteLine("Command received -> {0} ", data);
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
                Console.WriteLine(e.ToString());
            }
        }
    }
}