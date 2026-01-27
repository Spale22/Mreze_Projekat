using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace BranchOffice
{
    public class BranchOffice
    {
        private const int BUFFER_SIZE = 8192;
        private const int SRV_PORT = 15000;
        private static string enc_key = "";
        static void Main(string[] args)
        {

            Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint serverEP = new IPEndPoint(IPAddress.Loopback, SRV_PORT);

            Socket branchSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            Console.WriteLine("Branch office started.");
            Console.WriteLine("-----------------------------");
            Console.WriteLine("Press Ctrl+C to exit.");
            Console.WriteLine("-----------------------------");

            int port = -1;
            while (port < 0 || port > 65535)
            {
                Console.Write("Input Branch UDP socket port (0-65535):");
                string portInput = Console.ReadLine();
                Int32.TryParse(portInput, out port);
                if (port > 0 && port < 65535)
                    break;
                Console.WriteLine("Invalid port. Please enter a number between 0 and 65535.");
            }

            IPEndPoint localEP = new IPEndPoint(IPAddress.Loopback, port);
            branchSocket.Bind(localEP);
            branchSocket.Blocking = false;

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

                enc_key = Encoding.UTF8.GetString(buffer, 0, bytes);
                Console.WriteLine("Received enc_key from server");
            }
        }

        private static void Polling(Socket serverSocket, Socket branchSocket, int timeout, List<EndPoint> knownClientEPs, Queue<EndPoint> pendingReplies)
        {
            byte[] buffer = new byte[BUFFER_SIZE];
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

                byte[] keyBytes = Encoding.UTF8.GetBytes(enc_key);
                branchSocket.SendTo(keyBytes, tempEP);
                Console.WriteLine($"Sent enc_key to new client {tempEP}");
            }

            if (bytesReceived == 1 && buffer[0] == 0x00)
                return;

            byte[] data = new byte[bytesReceived];
            Buffer.BlockCopy(buffer, 0, data, 0, bytesReceived);

            Console.WriteLine($"Forwarding request from client {tempEP.ToString()} to server.");

            serverSocket.Send(data);

            pendingReplies.Enqueue(tempEP);
        }

        private static void Multiplexing(Socket serverSocket, Socket branchSocket, int timeout, Queue<EndPoint> pendingReplies)
        {
            List<Socket> checkRead = new List<Socket>() { serverSocket };
            List<Socket> checkError = new List<Socket>() { serverSocket };

            Socket.Select(checkRead, null, checkError, 1_000_000);

            if (checkRead.Count == 0 && checkError.Count == 0)
                return;

            if (checkError.Count > 0)
            {
                foreach (Socket s in checkError)
                {
                    Console.WriteLine($"Socket error on {s.RemoteEndPoint}, closing socket.");
                    s.Close();
                }
            }

            byte[] recvBuffer = new byte[BUFFER_SIZE];

            foreach (Socket s in checkRead)
            {
                try
                {
                    int bytesRead = s.Receive(recvBuffer);

                    if (bytesRead == 0)
                        continue;

                    if (pendingReplies.Count == 0)
                    {
                        Console.WriteLine("Warning: received server response with no pending client. Dropping.");
                        continue;
                    }

                    EndPoint targetClient = pendingReplies.Dequeue();

                    byte[] frame = new byte[bytesRead];
                    Buffer.BlockCopy(recvBuffer, 0, frame, 0, bytesRead);

                    Console.WriteLine($"Forwarding response from server to client {targetClient.ToString()}.");

                    branchSocket.SendTo(frame, targetClient);
                }
                catch (SocketException ex)
                {
                    Console.WriteLine($"Socket receive error: {ex.Message}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Unexpected error while handling socket: {ex.Message}");
                }
            }
        }
    }
}
