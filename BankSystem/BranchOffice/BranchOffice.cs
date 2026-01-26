using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace BranchOffice
{
    public class BranchOffice
    {
        private static string enc_key = "";
        static void Main(string[] args)
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

            try
            {
                serverSocket.Connect(serverEP);
                Console.WriteLine("Connected to server successfully.");
                serverSocket.Blocking = false;

                ReceiveServerEncKey(serverSocket);

                int timeout = 500 * 1000;

                List<EndPoint> knownClients = new List<EndPoint>();
                Queue<EndPoint> pendingReplies = new Queue<EndPoint>();

                while (!cts.IsCancellationRequested)
                {
                    Polling(serverSocket, branchSocket, timeout, knownClients, pendingReplies);
                    Multiplexing(serverSocket, branchSocket, timeout, pendingReplies);
                }
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"Failed to connect to server at startup.\nError:{ex.Message}");
            }
            finally
            {
                branchSocket.Close();
                serverSocket.Close();
                Console.WriteLine("Branch office shutting down...");
                Console.ReadKey();
            }
        }

        private static void ReceiveServerEncKey(Socket serverSocket)
        {
            byte[] buffer = new byte[37];
            int timeoutMicros = 60_000_000;

            if (serverSocket.Poll(timeoutMicros, SelectMode.SelectRead))
            {
                int bytes = serverSocket.Receive(buffer);
                if (bytes == 0)
                    throw new Exception("Connection closed by server while receiving encryption key.");
                enc_key = System.Text.Encoding.UTF8.GetString(buffer, 0, bytes);
                Console.WriteLine("Received enc_key from server");
            }
        }

        private static void Polling(Socket serverSocket, Socket branchSocket, int timeout, List<EndPoint> knownClientEPs, Queue<EndPoint> pendingReplies)
        {
            byte[] buffer = new byte[8192];
            EndPoint tempEP = new IPEndPoint(IPAddress.Any, 0);

            if (!branchSocket.Poll(timeout, SelectMode.SelectRead))
                return;

            int bytesReceived = branchSocket.ReceiveFrom(buffer, ref tempEP);
            if (bytesReceived == 0)
                return;

            if (!knownClientEPs.Contains(tempEP))
            {
                knownClientEPs.Add(tempEP);
                Console.WriteLine($"New client detected: {tempEP}");

                byte[] keyBytes = System.Text.Encoding.UTF8.GetBytes(enc_key);
                branchSocket.SendTo(keyBytes, tempEP);
                Console.WriteLine($"Sent enc_key to new client {tempEP}");
            }

            if (bytesReceived == 1 && buffer[0] == 0x00)
                return;

            byte[] data = new byte[bytesReceived];
            Buffer.BlockCopy(buffer, 0, data, 0, bytesReceived);
            serverSocket.Send(data);

            pendingReplies.Enqueue(tempEP);
        }

        private static void Multiplexing(Socket serverSocket, Socket branchSocket, int timeout, Queue<EndPoint> pendingReplies)
        {
            if (!serverSocket.Poll(timeout, SelectMode.SelectRead))
                return;

            byte[] recvBuffer = new byte[8192];
            int bytesRead = serverSocket.Receive(recvBuffer);

            if (bytesRead == 0)
            {
                Console.WriteLine($"Server closed connection: {serverSocket.RemoteEndPoint}");
                serverSocket.Close();
                return;
            }

            if (pendingReplies.Count == 0)
            {
                Console.WriteLine("Warning: received server response with no pending client. Dropping.");
                return;
            }

            EndPoint targetClient = pendingReplies.Dequeue();

            byte[] frame = new byte[bytesRead];
            Buffer.BlockCopy(recvBuffer, 0, frame, 0, bytesRead);
            branchSocket.SendTo(frame, targetClient);
        }
    }
}
