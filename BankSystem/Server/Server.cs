using Domain;
using Infrastructure;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Server
{
    public class Server
    {
        static readonly IClientRepository clientRepository = new ClientRepository();
        static readonly ITransactionRepository transactionRepository = new TransactionRepository(clientRepository);
        static readonly AuthenticationService authenticationService = new AuthenticationService(clientRepository);
        const int maxClients = 10;
        static void Main(string[] args)
        {
            DatabaseSeeder.Seed(clientRepository, transactionRepository);
            Run();
        }

        static void Run()
        {
            Socket listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint listenEndPoint = new IPEndPoint(IPAddress.Loopback, 15000);
            listenSocket.Blocking = false;
            listenSocket.Bind(listenEndPoint);
            listenSocket.Listen(maxClients);

            try
            {
                Multiplexing(listenSocket);
            }
            catch
            (Exception ex)
            {
                Console.WriteLine($"Server error: {ex.Message}");
            }
            finally
            {
                listenSocket.Close();
                Console.WriteLine("Shutting down server...");
                Console.ReadKey();
            }
        }

        private static void Multiplexing(Socket listener)
        {
            var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
                Console.WriteLine("Shutdown requested (Ctrl+C).");
            };

            List<Socket> readSockets = new List<Socket>();

            byte[] recvBuffer = new byte[8192];
            try
            {
                while (!cts.IsCancellationRequested)
                {
                    List<Socket> checkRead = new List<Socket>();
                    List<Socket> checkError = new List<Socket>();

                    if (readSockets.Count < maxClients)
                        checkRead.Add(listener);

                    checkError.Add(listener);

                    foreach (Socket s in readSockets)
                    {
                        checkRead.Add(s);
                        checkError.Add(s);
                    }

                    Socket.Select(checkRead, null, checkError, 1_000_000);

                    if (checkRead.Count == 0 && checkError.Count == 0)
                        continue;

                    if (checkError.Count > 0)
                    {
                        foreach (Socket s in checkError)
                        {
                            Console.WriteLine($"Socket error on {s.RemoteEndPoint}, closing socket.");
                            s.Close();
                            readSockets.Remove(s);
                        }
                    }

                    foreach (Socket s in checkRead)
                    {
                        if (s == listener)
                        {
                            if (readSockets.Count >= maxClients)
                                continue;
                            Socket clientSocket = listener.Accept();
                            clientSocket.Blocking = false;
                            readSockets.Add(clientSocket);
                            Console.WriteLine($"New client connected: {clientSocket.RemoteEndPoint}");
                            continue;
                        }

                        int bytesRead = s.Receive(recvBuffer);

                        if (bytesRead == 0)
                        {
                            Console.WriteLine($"Client disconnected: {s.RemoteEndPoint}");
                            s.Close();
                            checkRead.Remove(s);
                            continue;
                        }
                        byte[] frame = new byte[bytesRead];
                        Buffer.BlockCopy(recvBuffer, 0, frame, 0, bytesRead);

                        HandleRequest(s, frame, bytesRead);
                    }

                    checkRead.Clear();
                    checkError.Clear();
                }
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"Socket error ERROR CODE : {ex.SocketErrorCode}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            finally
            {
                foreach (var s in readSockets)
                {
                    s.Close();
                }
            }

        }

        private static void HandleRequest(Socket s, byte[] buffer, int bytesRead)
        {
            PackageType pkgType;
            Object obj;
            (pkgType, obj) = SerializationHelper.Deserialize<object>(buffer);
            switch (pkgType)
            {
                case PackageType.AuthRequest:
                    AuthRequestDTO authDto = (AuthRequestDTO)obj;
                    HandleAuthRequest(s, authDto);
                    break;
                case PackageType.TransactionRequest:
                    Transaction transactionDto = (Transaction)obj; ;
                    HandleTransactionRequest(s, transactionDto);
                    break;
                case PackageType.BalanceInquiryRequest:
                    Guid clientId = (Guid)obj;
                    HandleBalanceInquiryRequest(s, clientId);
                    break;
                default:
                    IPEndPoint senderEP = (IPEndPoint)s.LocalEndPoint;
                    Console.WriteLine($"Unknown package type received, from {senderEP.Address}:{senderEP.Port}");
                    break;
            }
        }

        private static void HandleBalanceInquiryRequest(Socket s, Guid clientId)
        {
            try
            {
                double balance = clientRepository.GetClientBalance(clientId);
                byte[] responseBytes = SerializationHelper.Serialize(PackageType.BalanceInquiryResponse, balance);
                s.Send(responseBytes);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Balance inquiry error: {ex.Message}");
                s.Send(SerializationHelper.Serialize(PackageType.MessageNotification, ex.Message));
            }
        }
        private static void HandleAuthRequest(Socket s, AuthRequestDTO dto)
        {
            try
            {
                User result = authenticationService.Authenticate(dto);
                byte[] responseBytes = SerializationHelper.Serialize(PackageType.AuthResponse, result);
                s.Send(responseBytes);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Authentication error: {ex.Message}");
                s.Send(SerializationHelper.Serialize(PackageType.MessageNotification, ex.Message));
            }
        }

        private static void HandleTransactionRequest(Socket s, Transaction dto)
        {
            try
            {
                bool result = transactionRepository.Create(dto);
                byte[] responseBytes = SerializationHelper.Serialize(PackageType.TransactionResponse, result);
                s.Send(responseBytes);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Transaction processing error: {ex.Message}");
                s.Send(SerializationHelper.Serialize(PackageType.MessageNotification, ex.Message));
            }
        }
    }
}
