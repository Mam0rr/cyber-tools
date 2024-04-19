using PacketDotNet;
using SharpPcap;

class PacketSniffer
{
    private static void device_OnPacketArrival(object sender, PacketCapture e)
    {
        var time = e.Header.Timeval.Date;
        var rawPacket = e.GetPacket();
        var packet = Packet.ParsePacket(rawPacket.LinkLayerType, rawPacket.Data);

        if (packet is EthernetPacket ethernetPacket)
        {
            Console.WriteLine($"Source MAC Address: {ethernetPacket.SourceHardwareAddress}      Destination MAC Address: {ethernetPacket.DestinationHardwareAddress}      Time: {time.Hour}:{time.Minute}:{time.Second}.{time.Millisecond}");
            Console.WriteLine($"   LinkedLayerType: {rawPacket.LinkLayerType}          Packet Length: {rawPacket.PacketLength}");
            Console.WriteLine("------------------------------------------------------------------------------------------------------");
        }

        if (packet is InternetPacket internetPacket)
        {
            Console.WriteLine($"Header Data: {internetPacket.HeaderData}      Payload Data: {internetPacket.PayloadData}      Time: {time.Hour}:{time.Minute}:{time.Second}.{time.Millisecond}");
            Console.WriteLine($"   LinkedLayerType: {rawPacket.LinkLayerType}          Packet Length: {rawPacket.PacketLength}");
            Console.WriteLine("------------------------------------------------------------------------------------------------------");
        }
    }

    static void Main(string[] args)
    {
        bool exiting = false;

        Console.WriteLine("  _____           _        _      _____       _  __  __          \r\n |  __ \\         | |      | |    / ____|     (_)/ _|/ _|         \r\n | |__) |_ _  ___| | _____| |_  | (___  _ __  _| |_| |_ ___ _ __ \r\n |  ___/ _` |/ __| |/ / _ \\ __|  \\___ \\| '_ \\| |  _|  _/ _ \\ '__|\r\n | |  | (_| | (__|   <  __/ |_   ____) | | | | | | | ||  __/ |   \r\n |_|   \\__,_|\\___|_|\\_\\___|\\__| |_____/|_| |_|_|_| |_| \\___|_|   \r\n                                                                 \r\n                                                                 \n\n\n\n");

        while (exiting == false)
        {

            Console.WriteLine("\nFor a list of commands, type 'help'\n");

            string cmd = Console.ReadLine();

            string[] parts = cmd.Split(' ');


            switch (parts[0])
            {
                case "run":
                    {
                        if (parts.Length == 1)
                        {
                            Console.WriteLine("Scanning for available devices...\n");

                            var devices = CaptureDeviceList.Instance;

                            if (devices.Count < 1)
                            {
                                Console.WriteLine("No devices were found on this machine");
                                return;
                            }

                            Console.WriteLine("\nThe following devices are available on this machine:");
                            Console.WriteLine("----------------------------------------------------\n");

                            int i = 0;

                            foreach (var dev in devices)
                            {
                                Console.WriteLine("{0} Name: {1}   MAC: {2}    Description: {3}", i, dev.Name, dev.MacAddress, dev.Description);
                                i++;
                            }

                            Console.WriteLine();
                            Console.Write("-- Please choose a device to capture: ");
                            i = int.Parse(Console.ReadLine());

                            using var device = devices[i];

                            device.OnPacketArrival +=
                                new PacketArrivalEventHandler(device_OnPacketArrival);

                            int readTimeoutMilliseconds = 1000;
                            device.Open(mode: DeviceModes.Promiscuous | DeviceModes.DataTransferUdp | DeviceModes.NoCaptureLocal, read_timeout: readTimeoutMilliseconds);

                            Console.WriteLine("\n-- Listening on {0} {1}, hit 'Enter' to stop...",
                                device.Name, device.Description);

                            Console.WriteLine("------------------------------------------------------------------------------------------------------");


                            device.StartCapture();

                            Console.ReadLine();

                            device.StopCapture();

                            Console.WriteLine("-- Capture stopped.");

                            Console.WriteLine($"-- Packets Received: {device.Statistics.ReceivedPackets}, Dopped Packets: {device.Statistics.DroppedPackets}, Interface Dropped Packets: {device.Statistics.InterfaceDroppedPackets}");


                            break;
                        }
                        else if (parts.Length == 2)
                        {

                            break;
                        }
                        else if (parts.Length == 3)
                        {

                            break;
                        }
                        else
                        {
                            break;
                        }

                    }
                case "exit":
                    {
                        Console.WriteLine("Exiting program...");
                        exiting = true;
                        break;
                    }
                case "help":
                    {
                        Console.WriteLine("\nAvailable Commands:");
                        Console.WriteLine("-----------------------------------------------------------------------------------");
                        Console.WriteLine("run             Starts capturing packets from a chosen network interface.\n");
                        Console.WriteLine("exit            Exits the program.\n");
                        Console.WriteLine("help            Displays a list of available commands and their usage.");
                        Console.WriteLine("-----------------------------------------------------------------------------------\n");
                        break;
                    }

            }

        }
    }
}
