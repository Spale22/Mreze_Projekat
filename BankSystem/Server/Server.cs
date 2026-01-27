using Domain;
using Infrastructure;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Server
{
    public class Server
    {
        static readonly IClientRepository clientRepository = new ClientRepository();
        static readonly ITransactionRepository transactionRepository = new TransactionRepository(clientRepository);
        static readonly AuthenticationService authenticationService = new AuthenticationService(clientRepository);
        const int maxClients = 10;

        private static string enc_key = "";
        static void Main(string[] args)
        {
            DatabaseSeeder.Seed(clientRepository, transactionRepository);

            Socket listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint listenEndPoint = new IPEndPoint(IPAddress.Loopback, 15000);
            listenSocket.Blocking = false;
            listenSocket.Bind(listenEndPoint);
            listenSocket.Listen(maxClients);

            try
            {
                while (string.IsNullOrWhiteSpace(enc_key) || enc_key.Length > 36)
                {
                    Console.WriteLine("Input communication encryption key ([A-Z] [0-9] max_length 36):");
                    enc_key = Console.ReadLine();
                }
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
                            clientSocket.Send(Encoding.UTF8.GetBytes(enc_key));
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

                        byte[] data = Encryptor.Decrypt(enc_key, frame);
                        if (data == null || data.Length == 0)
                            throw new Exception("Decryption failed or resulted in empty data.");

                        HandleRequest(s, data, bytesRead);
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
            Object obj = SerializationHelper.Deserialize<object>(buffer);
            switch (obj)
            {
                case AuthRequestDTO authDto:
                    HandleAuthRequest(s, authDto);
                    break;
                case Transaction transactionDto:
                    HandleTransactionRequest(s, transactionDto);
                    break;
                case Guid clientId:
                    HandleBalanceInquiryRequest(s, clientId);
                    break;
                case null:
                    Console.WriteLine("Received null package payload.");
                    break;
                default:
                    IPEndPoint senderEP = s.RemoteEndPoint as IPEndPoint;
                    string addr = senderEP != null ? $"{senderEP.Address}:{senderEP.Port}" : "unknown endpoint";
                    Console.WriteLine($"Unknown package type received ({obj.GetType().FullName}), from {addr}");
                    break;
            }
        }

        private static void HandleBalanceInquiryRequest(Socket s, Guid clientId)
        {
            try
            {
                double balance = clientRepository.GetClientBalance(clientId);
                byte[] responseBytes = SerializationHelper.Serialize(balance);
                byte[] data = Encryptor.Encrypt(enc_key, responseBytes);
                if (data == null || data.Length == 0)
                    throw new Exception("Encryption failed or resulted in empty data.");

                s.Send(data);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Balance inquiry error: {ex.Message}");
                byte[] msgBytes = SerializationHelper.Serialize(ex.Message);
                byte[] data = Encryptor.Encrypt(enc_key, msgBytes);
                if (data == null || data.Length == 0)
                    throw new Exception("Encryption failed or resulted in empty data.");

                s.Send(data);
            }
        }
        private static void HandleAuthRequest(Socket s, AuthRequestDTO dto)
        {
            try
            {
                User result = authenticationService.Authenticate(dto);
                byte[] responseBytes = SerializationHelper.Serialize(result);
                byte[] data = Encryptor.Encrypt(enc_key, responseBytes);
                if (data == null || data.Length == 0)
                    throw new Exception("Encryption failed or resulted in empty data.");

                s.Send(data);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Authentication error: {ex.Message}");
                byte[] msgBytes = SerializationHelper.Serialize(ex.Message);
                byte[] data = Encryptor.Encrypt(enc_key, msgBytes);
                if (data == null || data.Length == 0)
                    throw new Exception("Encryption failed or resulted in empty data.");

                s.Send(data);
            }
        }

        private static void HandleTransactionRequest(Socket s, Transaction dto)
        {
            try
            {
                bool result = transactionRepository.Create(dto);
                byte[] responseBytes = SerializationHelper.Serialize(result);
                byte[] data = Encryptor.Encrypt(enc_key, responseBytes);
                if (data == null || data.Length == 0)
                    throw new Exception("Encryption failed or resulted in empty data.");

                s.Send(data);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Transaction processing error: {ex.Message}");
                byte[] msgBytes = SerializationHelper.Serialize(ex.Message);
                byte[] data = Encryptor.Encrypt(enc_key, msgBytes);
                if (data == null || data.Length == 0)
                    throw new Exception("Encryption failed or resulted in empty data.");

                s.Send(data);
            }
        }
    }
}
