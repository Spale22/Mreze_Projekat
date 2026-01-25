using System;
using System.Net;
using System.Net.Sockets;

namespace BranchOffice
{
    public class BranchOffice
    {
        static void Main(string[] args)
        {
            Socket branchSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            IPEndPoint localEP = new IPEndPoint(IPAddress.Loopback, 16001);
            branchSocket.Bind(localEP);
            branchSocket.Blocking = false;

            Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint serverEP = new IPEndPoint(IPAddress.Loopback, 15000);
            serverSocket.Blocking = false;
            try
            {
                serverSocket.Connect(serverEP);
                Console.WriteLine("Connected to server successfully.");
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"Failed to connect to server at startup.\nError:{ex.Message}");
            }

            int timeout = 500 * 1000;

            Polling(serverSocket, branchSocket, timeout);

            branchSocket.Close();
            serverSocket.Close();
            Console.WriteLine("Branch office shutting down...");
            Console.ReadKey();
        }

        private static void Polling(Socket serverSocket, Socket branchSocket, int timeout)
        {
            var cts = new System.Threading.CancellationTokenSource();

            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
                Console.WriteLine("Shutdown requested (Ctrl+C).");
            };

            try
            {
                byte[] buffer = new byte[8192];
                EndPoint clientEP = new IPEndPoint(IPAddress.Any, 0);

                while (!cts.IsCancellationRequested)
                {
                    if (branchSocket.Poll(timeout, SelectMode.SelectRead))
                    {
                        int bytesReceived = branchSocket.ReceiveFrom(buffer, ref clientEP);
                        byte[] data = new byte[bytesReceived];
                        Buffer.BlockCopy(buffer, 0, data, 0, bytesReceived);
                        serverSocket.Send(data);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}
