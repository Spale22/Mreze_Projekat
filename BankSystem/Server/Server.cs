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
        static readonly IUserRepository userRepository = new UserRepository();
        static readonly ITransactionRepository transactionRepository = new TransactionRepository(userRepository);
        static readonly AuthenticationService authenticationService = new AuthenticationService(userRepository);
        private static readonly List<Socket> clientSockets = new List<Socket>();
        private const int BUFFER_SIZE = 8192;
        private const int PORT = 15000;
        private const int MaxClient = 100;
        private static string enc_key = "";
        static void Main(string[] args)
        {
            DatabaseSeeder.Seed(userRepository, transactionRepository);


            Socket listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint listenEndPoint = new IPEndPoint(IPAddress.Loopback, PORT);
            listenSocket.Blocking = false;
            listenSocket.Bind(listenEndPoint);
            listenSocket.Listen(MaxClient);

            Console.WriteLine("Server started.");
            Console.WriteLine("-----------------------------");
            Console.WriteLine("Press Ctrl+C to exit.");
            Console.WriteLine("-----------------------------");

            try
            {
                while (string.IsNullOrWhiteSpace(enc_key) || enc_key.Length > 36)
                {
                    Console.WriteLine("Input communication encryption key ([A-Z] [0-9] max_length 36):");
                    enc_key = Console.ReadLine();
                }

                Console.WriteLine("Key accepted.");
                Console.WriteLine("Listening for connections...");

                var cts = new CancellationTokenSource();
                Console.CancelKeyPress += (sender, e) =>
                {
                    e.Cancel = true;
                    cts.Cancel();
                    Console.WriteLine("Shutdown requested (Ctrl+C).");
                };
                while (!cts.IsCancellationRequested)
                    Multiplexing(listenSocket);
            }
            catch
            (Exception ex)
            {
                Console.WriteLine($"Server error: {ex.Message}");
            }
            finally
            {
                foreach (var s in clientSockets)
                    s.Close();

                listenSocket.Close();
                Console.WriteLine("Shutting down server...");
                Console.ReadKey();
            }
        }

        private static void Multiplexing(Socket listener)
        {
            byte[] recvBuffer = new byte[BUFFER_SIZE];
            List<Socket> checkRead = new List<Socket>() { listener };
            List<Socket> checkError = new List<Socket>() { listener };

            foreach (Socket s in clientSockets)
            {
                checkRead.Add(s);
                checkError.Add(s);
            }

            try
            {
                Socket.Select(checkRead, null, checkError, 1_000_000);

                if (checkRead.Count == 0 && checkError.Count == 0)
                    return;

                if (checkError.Count > 0)
                {
                    foreach (Socket s in checkError)
                    {
                        Console.WriteLine($"Socket error on {s.RemoteEndPoint}, closing socket.");
                        s.Close();
                        clientSockets.Remove(s);
                    }
                }

                foreach (Socket s in checkRead)
                {
                    if (s == listener)
                    {
                        Socket clientSocket = null;

                        try
                        {
                            clientSocket = listener.Accept();
                            clientSocket.Blocking = false;
                            clientSockets.Add(clientSocket);

                            Console.WriteLine($"New client connected: {clientSocket.RemoteEndPoint}");
                            clientSocket.Send(Encoding.UTF8.GetBytes(enc_key));
                        }
                        catch (SocketException ex)
                        {
                            Console.WriteLine($"Accept failed: {ex.SocketErrorCode}");
                            if (clientSocket != null)
                                clientSocket.Close();
                        }
                        continue;
                    }

                    int bytesRead = s.Receive(recvBuffer);

                    if (bytesRead == 0)
                    {
                        Console.WriteLine($"Client disconnected: {s.RemoteEndPoint}");
                        s.Close();
                        clientSockets.Remove(s);
                        continue;
                    }

                    byte[] frame = new byte[bytesRead];
                    Buffer.BlockCopy(recvBuffer, 0, frame, 0, bytesRead);

                    byte[] data = Encryptor.Decrypt(enc_key, frame);
                    if (data == null || data.Length == 0)
                        throw new Exception("Decryption failed or resulted in empty data.");

                    HandleRequest(s, data, bytesRead);
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
                double balance = userRepository.GetClientBalance(clientId);
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
