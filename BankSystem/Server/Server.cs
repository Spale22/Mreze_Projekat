using Domain.DTOs;
using Domain.HelperMethods;
using Domain.Interfaces.Services;
using Server.Services;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Server
{
    public class Server
    {
        static readonly ITransactionService transactionService = new TransactionService();
        static readonly IAuthenticationService authenticationService = new AuthenticationService();
        static readonly IClientDataService clientDataService = new ClientDataService();

        static void Main(string[] args)
        {
            Socket authSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            Socket transactionSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            Socket clientDataSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            authSocket.Blocking = false;
            transactionSocket.Blocking = false;
            clientDataSocket.Blocking = false;

            IPEndPoint authReqEndPoint = new IPEndPoint(IPAddress.Loopback, 15001);
            IPEndPoint transactionReqEndPoint = new IPEndPoint(IPAddress.Loopback, 15002);
            IPEndPoint clientDataReqEndPoint = new IPEndPoint(IPAddress.Loopback, 15003);

            authSocket.Bind(authReqEndPoint);
            transactionSocket.Bind(transactionReqEndPoint);
            clientDataSocket.Bind(clientDataReqEndPoint);

            List<Socket> readSockets = new List<Socket>() { authSocket, transactionSocket, clientDataSocket };

            try
            {
                Multiplexing(readSockets);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
            finally
            {
                authSocket.Close();
                transactionSocket.Close();
                clientDataSocket.Close();
                Console.WriteLine("Shutting down server...");
            }
        }

        private static void Multiplexing(List<Socket> readSockets)
        {
            var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
                Console.WriteLine("Shutdown requested (Ctrl+C).");
            };

            byte[] recvBuffer = new byte[8192];
            while (!cts.IsCancellationRequested)
            {
                List<Socket> selectSockets = new List<Socket>(readSockets);

                Socket.Select(selectSockets, null, null, 1_000_000);

                foreach (Socket s in selectSockets)
                {
                    try
                    {
                        int bytesRead = s.Receive(recvBuffer);
                        if (bytesRead > 0)
                        {
                            int localPort = ((IPEndPoint)s.LocalEndPoint).Port;
                            switch (localPort)
                            {
                                case 15001:
                                    Console.WriteLine($"[Auth] Received {bytesRead} bytes");
                                    HandleAuthRequest(s, recvBuffer, bytesRead);
                                    break;
                                case 15002:
                                    Console.WriteLine($"[Transaction] Received {bytesRead} bytes");
                                    HandleTransactionRequest(s, recvBuffer, bytesRead);
                                    break;
                                case 15003:
                                    Console.WriteLine($"[ClientData] Received {bytesRead} bytes");
                                    HandleClientDataRequest(s, recvBuffer, bytesRead);
                                    break;
                            }
                        }
                    }
                    catch (SocketException ex)
                    {
                        if (ex.SocketErrorCode == SocketError.WouldBlock)
                        {
                            continue;
                        }
                        Console.WriteLine($"Socket error on {((IPEndPoint)s.LocalEndPoint).Port}: {ex.SocketErrorCode}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Socket error on {((IPEndPoint)s.LocalEndPoint).Port}: {ex.Message}");
                    }
                    selectSockets.Clear();
                }
            }
        }

        private static void HandleAuthRequest(Socket s, byte[] buffer, int count)
        {
            AuthRequestDTO dto = SerializationHelper.Deserialize<AuthRequestDTO>(buffer);
            AuthResponseDTO result = authenticationService.Authenticate(dto);
            byte[] responseBytes = SerializationHelper.Serialize(result);
            s.Send(responseBytes);
        }

        private static void HandleTransactionRequest(Socket s, byte[] buffer, int count)
        {
            TransactionRequestDTO dto = SerializationHelper.Deserialize<TransactionRequestDTO>(buffer);
            TransactionResponseDTO result = transactionService.ProcessTransaction(dto);
            byte[] responseBytes = SerializationHelper.Serialize(result);
            s.Send(responseBytes);
        }

        private static void HandleClientDataRequest(Socket s, byte[] buffer, int count)
        {
            ClientDataRequestDTO dto = SerializationHelper.Deserialize<ClientDataRequestDTO>(buffer);
            ClientDataResponseDTO result = clientDataService.GetClientData(dto);
            byte[] responseBytes = SerializationHelper.Serialize(result);
            s.Send(responseBytes);
        }
    }
}
