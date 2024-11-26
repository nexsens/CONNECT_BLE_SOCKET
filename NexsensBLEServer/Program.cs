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
        static void Main(string[] args)
        {
            ExecuteServer();
        }

        public static async Task SearchDevices()
        {
            await Task.Run(()=>SearchDevicesAsync());
        }
        public static async Task SearchDevicesAsync()
        {
            try
            {
                var discoveredDevices = await Bluetooth.ScanForDevicesAsync();
                Console.WriteLine($"found {discoveredDevices?.Count} devices");
                foreach(var d in discoveredDevices)
                {
                    Console.WriteLine("Name:"+d.Name+" | ID"+d.Id);
                }
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
        
        public static int Command_Handler(string command)
        {
            if (command == "stop")
                return -1;
            
            else if (command == "connect")
            {
                Console.WriteLine("Connecting to device...");
            }
            else if (command == "scan")
            {
                Console.WriteLine("Scanning..");

                SearchDevices();

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
            IPHostEntry ipHost = Dns.GetHostEntry(Dns.GetHostName());
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
                    Console.WriteLine("Connected!");

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
                        Console.WriteLine("Text received -> {0} ", data);
                        byte[] message = Encoding.ASCII.GetBytes(data);
                        clientSocket.Send(message);
                        ret = Command_Handler(data);
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