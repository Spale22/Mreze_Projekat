using System;
using Domain.DTOs;
using Domain.HelperMethods;
using System.Net;
using System.Net.Sockets;

namespace Client
{
    public class Client
    {
        static void Main(string[] args)
        {
            Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            clientSocket.ReceiveTimeout = 2000;

            IPEndPoint branchEP = new IPEndPoint(IPAddress.Loopback, 15001);

            Console.Write("Operation: ");
            string operation = Console.ReadLine() ?? "";

            Console.Write("Amount: ");
            double amount = 0.0;
            double.TryParse(Console.ReadLine(), out amount);

            ClientDataRequestDTO request = new ClientDataRequestDTO
            {
                Operation = operation, 
                Amount = amount
            };

            byte[] sendBuffer = SerializationHelper.Serialize(request);
            clientSocket.SendTo(sendBuffer, branchEP);

            byte[] recvBuffer = new byte[8192];
            EndPoint senderEP = new IPEndPoint(IPAddress.Any, 0);

            try
            {
                int bytesReceived = clientSocket.ReceiveFrom(recvBuffer, ref senderEP);
                byte[] message = new byte[bytesReceived];
                Array.Copy(recvBuffer, message, bytesReceived);

                ClientDataResponseDTO response = SerializationHelper.Deserialize<ClientDataResponseDTO>(message);
                Console.WriteLine($"Response from server: Success={response.Success}, Message='{response.Message}'");
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"Socket error: {ex.Message}");
            }
            
            clientSocket.Close();
        }
    }
}
