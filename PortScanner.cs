using System.Net.Sockets;

class PortScanner
{
    static void Main(string[] args)
    {
        Console.Write("Enter the IP address to scan: ");
        string ipAddress = Console.ReadLine();

        Console.Write("Enter the starting port number: ");
        int startPort = int.Parse(Console.ReadLine());

        Console.Write("Enter the ending port number: ");
        int endPort = int.Parse(Console.ReadLine());

        Console.WriteLine($"Scanning ports for {ipAddress}...");

        List<int> openPorts = new List<int>();

        Parallel.For(startPort, endPort + 1, port =>
        {
            if (IsPortOpen(ipAddress, port))
            {
                lock (openPorts) 
                {
                    openPorts.Add(port);
                }
            }
        });

        if (openPorts.Count > 0)
        {
            openPorts.Sort();

            Console.WriteLine("Open ports:");
            foreach (int port in openPorts)
            {
                Console.WriteLine($"Port {port} is open");
            }
        }
        else
        {
            Console.WriteLine($"There are no open ports running on {ipAddress}");
        }
    }

    static bool IsPortOpen(string ipAddress, int port)
    {
        try
        {
            using (TcpClient tcpClient = new TcpClient())
            {
                tcpClient.Connect(ipAddress, port);
                return true;
            }
        }
        catch (SocketException)
        {
            return false;
        }
    }
}
