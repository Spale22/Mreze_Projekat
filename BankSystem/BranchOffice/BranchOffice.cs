using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace BranchOffice
{
    public class BranchOffice
    {
        static void Main(string[] args)
        {
            Run();
        }

        static void Run()
        {
            Socket branchSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            IPEndPoint localEP = new IPEndPoint(IPAddress.Loopback, 16001);
            branchSocket.Bind(localEP);
            branchSocket.Blocking = false;

            Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint serverEP = new IPEndPoint(IPAddress.Loopback, 15000);

            var cts = new CancellationTokenSource();

            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
                Console.WriteLine("Shutdown requested (Ctrl+C).");
            };

            int maxClients = 10;

            try
            {
                serverSocket.Connect(serverEP);
                Console.WriteLine("Connected to server successfully.");
                serverSocket.Blocking = false;
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"Failed to connect to server at startup.\nError:{ex.Message}");
            }

            int timeout = 500 * 1000;

            EndPoint clientEP = null;

            while (!cts.IsCancellationRequested)
            {
                Polling(serverSocket, branchSocket, timeout, ref clientEP);
                Multiplexing(serverSocket, branchSocket, maxClients, timeout, ref clientEP);
            }
            branchSocket.Close();
            serverSocket.Close();
            Console.WriteLine("Branch office shutting down...");
            Console.ReadKey();
        }

        private static void Polling(Socket serverSocket, Socket branchSocket, int timeout, ref EndPoint clientEP)
        {
            try
            {
                byte[] buffer = new byte[8192];
                EndPoint tempEP = new IPEndPoint(IPAddress.Any, 0);

                if (branchSocket.Poll(timeout, SelectMode.SelectRead))
                {
                    int bytesReceived = branchSocket.ReceiveFrom(buffer, ref tempEP);
                    if (bytesReceived == 0)
                        return;
                    clientEP = tempEP;
                    byte[] data = new byte[bytesReceived];
                    Buffer.BlockCopy(buffer, 0, data, 0, bytesReceived);
                    serverSocket.Send(data);
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
        private static void Multiplexing(Socket serverSocket, Socket branchSocket, int maxClients, int timeout, ref EndPoint clientEP)
        {
            
        }
    }
}
