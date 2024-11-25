using System;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.Sockets;
using System.Text;
//using Windows.Devices.Bluetooth;
//using Windows.Devices.Enumeration;
    //< PackageReference Include = "Microsoft.Windows.SDK.Contracts" Version = "10.0.26100.1742" />

namespace Server
{

    class Program
    {
        //static DeviceWatcher deviceWatcher;
        // Main Method
        static void Main(string[] args)
        {
            ExecuteServer();
            //ScanBLE();
        }

        public static void ScanBLE()
        {
            Console.WriteLine("Scanning nearby bluetooth LE devices...");
            //StartBleDeviceWatcher();
        }
        //public static void StartBleDeviceWatcher()

        //{
        //    // Additional properties we would like about the device.
        //    // Property strings are documented here https://msdn.microsoft.com/en-us/library/windows/desktop/ff521659(v=vs.85).aspx
        //    string[] requestedProperties = { "System.Devices.Aep.DeviceAddress", "System.Devices.Aep.IsConnected", "System.Devices.Aep.Bluetooth.Le.IsConnectable" };

        //    // BT_Code: Example showing paired and non-paired in a single query.
        //    string aqsAllBluetoothLEDevices = "(System.Devices.Aep.ProtocolId:=\"{bb7bb05e-5972-42b5-94fc-76eaa7084d49}\")";

        //    deviceWatcher =
        //            DeviceInformation.CreateWatcher(
        //                aqsAllBluetoothLEDevices,
        //                requestedProperties,
        //                DeviceInformationKind.AssociationEndpoint);

        //    // Register event handlers before starting the watcher.
        //    //deviceWatcher.Added += DeviceWatcher_Added;
        //    //deviceWatcher.Updated += DeviceWatcher_Updated;
        //    //deviceWatcher.Removed += DeviceWatcher_Removed;
        //    //deviceWatcher.EnumerationCompleted += DeviceWatcher_EnumerationCompleted;
        //    //deviceWatcher.Stopped += DeviceWatcher_Stopped;

        //    // Start over with an empty collection.
        //    //KnownDevices.Clear();

        //    // Start the watcher. Active enumeration is limited to approximately 30 seconds.
        //    // This limits power usage and reduces interference with other Bluetooth activities.
        //    // To monitor for the presence of Bluetooth LE devices for an extended period,
        //    // use the BluetoothLEAdvertisementWatcher runtime class. See the BluetoothAdvertisement
        //    // sample for an example.
        //    deviceWatcher.Start();
        //}

        ///// <summary>
        ///// Stops watching for all nearby Bluetooth devices.
        ///// </summary>
        //private void StopBleDeviceWatcher()
        //{
        //    if (deviceWatcher != null)
        //    {
        //        // Unregister the event handlers.
        //        //deviceWatcher.Added -= DeviceWatcher_Added;
        //        //deviceWatcher.Updated -= DeviceWatcher_Updated;
        //        //deviceWatcher.Removed -= DeviceWatcher_Removed;
        //        //deviceWatcher.EnumerationCompleted -= DeviceWatcher_EnumerationCompleted;
        //        //deviceWatcher.Stopped -= DeviceWatcher_Stopped;

        //        // Stop the watcher.
        //        deviceWatcher.Stop();
        //        deviceWatcher = null;
        //    }
        //}
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

                        Console.WriteLine("Text received -> {0} ", data);
                        byte[] message = Encoding.ASCII.GetBytes(data);

                        // Send a message to Client 
                        // using Send() method
                        clientSocket.Send(message);
                        data = "";

                    }
                    // Close client Socket using the
                    // Close() method. After closing,
                    // we can use the closed Socket 
                    // for a new Client Connection
                    clientSocket.Shutdown(SocketShutdown.Both);
                    clientSocket.Close();
                }
            }

            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
    }
}