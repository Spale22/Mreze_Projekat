using Domain.DTOs;
using Domain.HelperMethods;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

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

            int timeout = 500 * 1000;

            while(true)
            {
                if(branchSocket.Poll(timeout, SelectMode.SelectRead))
                {
                    try
                    {
                        byte[] buffer = new byte[8192];
                        EndPoint clientEP = new IPEndPoint(IPAddress.Any, 0);
                        int bytesReceived = branchSocket.ReceiveFrom(buffer, ref clientEP);

                        byte[] data = new byte[bytesReceived];
                        Array.Copy(buffer, data, bytesReceived);

                        ClientDataRequestDTO request = SerializationHelper.Deserialize<ClientDataRequestDTO>(data);
                        Console.WriteLine($"Request from {clientEP}: Operation: {request.Operation} Amount={request.Amount}");

                        ClientDataResponseDTO response = new ClientDataResponseDTO
                        {
                            Success = true,
                            Message = $"Processed {request.Operation} of amount {request.Amount}"
                        };

                        byte[] responseBytes = SerializationHelper.Serialize(response);
                        branchSocket.SendTo(responseBytes, clientEP);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Socket error on {(IPEndPoint)branchSocket.LocalEndPoint}: {ex.Message}");
                    }
                }
            }
        }
    }
}
