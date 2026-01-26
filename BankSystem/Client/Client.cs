using System;
using Domain;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Infrastructure;

namespace Client
{
    public class Client
    {
        static void Main(string[] args)
        {
            Console.WriteLine("UDP Client started.");
            Console.WriteLine("-----------------------------");
            Console.WriteLine("Press Ctrl+C to exit.");
            Console.WriteLine("-----------------------------");
            Run();
        }

        static void Run()
        {
            Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            
            IPEndPoint localEP = new IPEndPoint(IPAddress.Loopback, 0);
            clientSocket.Bind(localEP);
            clientSocket.Blocking = false;

            IPEndPoint branchEP = new IPEndPoint(IPAddress.Loopback, 16001);

            var cts = new System.Threading.CancellationTokenSource();

            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
                Console.WriteLine("Shutdown requested (Ctrl+C).");
            };

            User currentUser;
            do{
                currentUser = UserLogin(clientSocket, branchEP);
            } while(currentUser == null);

            while (!cts.IsCancellationRequested)
            {
                int opr = -1;
                do
                {
                    Console.WriteLine("Operation: ");
                    Console.WriteLine(" 1 - Balance inquiry");
                    Console.WriteLine(" 2 - Deposit");
                    Console.WriteLine(" 3 - Withdraw");
                    Console.WriteLine(" 4 - Transfer money");
                    Console.WriteLine("-----------------------------");
                    Console.Write("Select operation (1-4): ");
                    Int32.TryParse(Console.ReadLine(), out opr);
                    Console.WriteLine("-----------------------------");
                } while (opr < 0 || opr > 3);

                switch (opr)
                {
                    case 1:
                        Console.WriteLine($"Current balance: {currentUser.Balance}");
                        break;
                    case 2:
                        HandleDeposit(clientSocket, branchEP, ref currentUser);
                        break;
                    case 3:
                        HandleWithdraw(clientSocket, branchEP, ref currentUser);
                        break;
                    case 4:
                        HandleTransfer(clientSocket, branchEP, ref currentUser);
                        break;
                }
            }

            clientSocket.Close();
            Console.WriteLine("Client shuting down ...");
            Console.ReadKey();
        }

        private static Object SendAndAwaitResponse(Socket clientSocket, IPEndPoint branchEP, byte[] payload)
        {
            clientSocket.SendTo(payload, branchEP);
            Object result = null;

            int timeoutMicros = 2_000_000;
            byte[] buffer = new byte[8192];
            EndPoint remote = new IPEndPoint(IPAddress.Any, 0);

            if (clientSocket.Poll(timeoutMicros, SelectMode.SelectRead))
            {
                int bytesRead = clientSocket.ReceiveFrom(buffer, ref remote);
                if(bytesRead <= 0)
                    throw new Exception("Received zero bytes from server.");
                byte[] frame = new byte[bytesRead];
                Buffer.BlockCopy(buffer, 0, frame, 0, bytesRead);
                (_, result) = SerializationHelper.Deserialize<Object>(frame);                
            }
            else
            {
                Console.WriteLine("No response within timeout.");
            }
            return result;
        }
        private static void HandleDeposit(Socket clientSocket, IPEndPoint branchEP, ref User currentUser)
        {
            Console.Write("Input amount: ");
            double amount = Double.Parse(Console.ReadLine() ?? "");
            Transaction t = new Transaction(currentUser.UserId, currentUser.UserId, amount, TransactionType.Deposit);
            byte[] payload = SerializationHelper.Serialize(PackageType.TransactionRequest, t);

            try
            {
                TransactionResponseDTO dto = (TransactionResponseDTO)SendAndAwaitResponse(clientSocket, branchEP, payload);
                if(dto.Result)
                    Console.WriteLine("Deposit successful.");               
                else
                    Console.WriteLine("Deposit failed.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Deposit failed: {ex.Message}");
            }           
        }
        private static void HandleWithdraw(Socket clientSocket, IPEndPoint branchEP, ref User currentUser)
        {
            Console.Write("Input amount: ");
            double amount = Double.Parse(Console.ReadLine() ?? "");
            Transaction t = new Transaction(currentUser.UserId, currentUser.UserId, amount, TransactionType.Withdraw);
            byte[] payload = SerializationHelper.Serialize(PackageType.TransactionRequest, t);

            try
            {
                TransactionResponseDTO dto = (TransactionResponseDTO)SendAndAwaitResponse(clientSocket, branchEP, payload);
                if (dto.Result)
                    Console.WriteLine("Withdraw successful.");
                else
                    Console.WriteLine("Withdraw failed.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Withdraw failed: {ex.Message}");
            }
        }
        private static void HandleTransfer(Socket clientSocket, IPEndPoint branchEP, ref User currentUser)
        {
            throw new NotImplementedException();
        }
        private static User UserLogin(Socket clientSocket, IPEndPoint branchEP)
        {
            Console.WriteLine("User Login");
            Console.WriteLine("-----------------------------");
            Console.Write("Enter username: ");
            string username = Console.ReadLine();
            Console.Write("Enter password: ");
            string password = Console.ReadLine();
            
            AuthRequestDTO authRequest = new AuthRequestDTO
            {
                Username = username,
                Password = password
            };
            byte[] payload = SerializationHelper.Serialize(PackageType.AuthRequest , authRequest);
            try 
            {
                AuthResponseDTO dto = (AuthResponseDTO)SendAndAwaitResponse(clientSocket, branchEP, payload);
                return dto.LoggedClient;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Login failed: {ex.Message}");
                return null;
            }
        }
    }
}
