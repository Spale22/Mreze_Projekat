using Domain;
using Infrastructure;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Client
{
    public class Client
    {
        static void Main(string[] args)
        {
            Run();
        }

        static void Run()
        {
            Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            IPEndPoint localEP = new IPEndPoint(IPAddress.Loopback, 0);
            clientSocket.Bind(localEP);
            clientSocket.Blocking = false;

            IPEndPoint branchEP = new IPEndPoint(IPAddress.Loopback, 16001);

            var cts = new CancellationTokenSource();

            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
                Console.WriteLine("Shutdown requested (Ctrl+C).");
            };

            Console.WriteLine("UDP Client started.");
            Console.WriteLine("-----------------------------");
            Console.WriteLine("Press Ctrl+C to exit.");
            Console.WriteLine("-----------------------------");

            User currentUser = new User();
            try
            {
                while (!cts.IsCancellationRequested)
                {
                    while (currentUser.UserId == Guid.Empty && !cts.IsCancellationRequested)
                        currentUser = UserLogin(clientSocket, branchEP, ref cts);

                    Console.WriteLine("-----------------------------");
                    Console.WriteLine($"Welcome, {currentUser.FirstName} {currentUser.LastName}!");
                    Console.WriteLine("-----------------------------");

                    if (cts.IsCancellationRequested)
                        break;

                    int opr = -1;
                    do
                    {
                        Console.WriteLine("Operation: ");
                        Console.WriteLine(" 0 - Logout");
                        Console.WriteLine(" 1 - Balance inquiry");
                        Console.WriteLine(" 2 - Deposit");
                        Console.WriteLine(" 3 - Withdraw");
                        Console.WriteLine(" 4 - Transfer money");
                        Console.WriteLine("-----------------------------");
                        Console.Write("Select operation (1-4): ");
                        Int32.TryParse(Console.ReadLine(), out opr);
                        Console.WriteLine("-----------------------------");
                    } while ((opr < 0 || opr > 4) && !cts.IsCancellationRequested);

                    if (cts.IsCancellationRequested)
                        break;

                    switch (opr)
                    {
                        case 0:
                            Console.WriteLine("Logging out ...");
                            currentUser = new User();
                            break;
                        case 1:
                            HandleBalanceInquiry(clientSocket, branchEP, ref currentUser);
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
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred:\n {ex.Message}");
            }
            finally
            {
                clientSocket.Close();
                Console.WriteLine("Client shuting down ...");
                Console.ReadKey();
            }
        }

        private static Object SendAndAwaitResponse(Socket clientSocket, IPEndPoint branchEP, byte[] payload)
        {
            clientSocket.SendTo(payload, branchEP);
            Object result = null;
            PackageType pkgType;

            int timeoutMicros = 2_000_000;
            byte[] buffer = new byte[8192];
            EndPoint remote = new IPEndPoint(IPAddress.Any, 0);

            if (clientSocket.Poll(timeoutMicros, SelectMode.SelectRead))
            {
                int bytesRead = clientSocket.ReceiveFrom(buffer, ref remote);
                if (bytesRead <= 0)
                    throw new Exception("Received zero bytes from server.");
                byte[] frame = new byte[bytesRead];
                Buffer.BlockCopy(buffer, 0, frame, 0, bytesRead);
                (pkgType, result) = SerializationHelper.Deserialize<Object>(frame);
                if (pkgType == PackageType.MessageNotification)
                    throw new Exception("Message from server: " + (string)result);
            }
            else
                throw new Exception("No response within timeout.");
            
            return result;
        }
        private static void HandleBalanceInquiry(Socket clientSocket, IPEndPoint branchEP, ref User currentUser)
        {
            Console.WriteLine("Balance Inquiry Operation");
            Console.WriteLine("-----------------------------");
            byte[] payload = SerializationHelper.Serialize(PackageType.BalanceInquiryRequest, currentUser.UserId);
            try
            {
                double balance = (double)SendAndAwaitResponse(clientSocket, branchEP, payload);
                if (balance < 0)
                {
                    Console.WriteLine("Balance inquiry failed.");
                    return;
                }
                Console.WriteLine($"Current balance: {balance}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Balance inquiry failed:\n {ex.Message}");
            }
        }
        private static void HandleDeposit(Socket clientSocket, IPEndPoint branchEP, ref User currentUser)
        {
            Console.WriteLine("Deposit Operation");
            Console.WriteLine("-----------------------------");
            Console.Write("Input amount: ");
            double amount = Double.Parse(Console.ReadLine() ?? "");
            Transaction t = new Transaction(currentUser.UserId, amount, DateTime.Now, TransactionType.Deposit);
            byte[] payload = SerializationHelper.Serialize(PackageType.TransactionRequest, t);

            try
            {
                bool result = (bool)SendAndAwaitResponse(clientSocket, branchEP, payload);
                if (result)
                    Console.WriteLine("Deposit successful.");
                else
                    Console.WriteLine("Deposit failed.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Deposit failed:\n {ex.Message}");
            }
        }
        private static void HandleWithdraw(Socket clientSocket, IPEndPoint branchEP, ref User currentUser)
        {
            Console.WriteLine("Withdraw Operation");
            Console.WriteLine("-----------------------------");
            Console.Write("Input amount: ");
            double amount = Double.Parse(Console.ReadLine() ?? "");
            Transaction t = new Transaction(currentUser.UserId, amount, DateTime.Now, TransactionType.Withdraw);
            byte[] payload = SerializationHelper.Serialize(PackageType.TransactionRequest, t);

            try
            {
                bool result = (bool)SendAndAwaitResponse(clientSocket, branchEP, payload);
                if (result)
                    Console.WriteLine("Withdraw successful.");
                else
                    Console.WriteLine("Withdraw failed.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Withdraw failed:\n {ex.Message}");
            }
        }
        private static void HandleTransfer(Socket clientSocket, IPEndPoint branchEP, ref User currentUser)
        {
            Console.WriteLine("Transfer Operation");
            Console.WriteLine("-----------------------------");
            Console.Write("Input amount: ");
            double amount = Double.Parse(Console.ReadLine() ?? "");
            Console.Write("Input recipient account number: ");
            string recipientAccountNumber = Console.ReadLine() ?? "";
            Transaction t = new Transaction(currentUser.UserId, amount, DateTime.Now, TransactionType.Transfer, recipientAccountNumber);
            byte[] payload = SerializationHelper.Serialize(PackageType.TransactionRequest, t);

            try
            {
                bool result = (bool)SendAndAwaitResponse(clientSocket, branchEP, payload);
                if (result)
                    Console.WriteLine("Trasfer successful.");
                else
                    Console.WriteLine("Transfer failed.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Transfer failed:\n {ex.Message}");
            }
        }
        private static User UserLogin(Socket clientSocket, IPEndPoint branchEP,ref CancellationTokenSource cts)
        {
            Console.WriteLine("User Login");
            Console.WriteLine("-----------------------------");
            Console.Write("Enter username: ");
            string username = Console.ReadLine();
            Console.Write("Enter password: ");
            string password = Console.ReadLine();

            if (cts.IsCancellationRequested)
                return new User();

            AuthRequestDTO authRequest = new AuthRequestDTO
            {
                Username = username,
                Password = password
            };
            byte[] payload = SerializationHelper.Serialize(PackageType.AuthRequest, authRequest);
            try
            {
                User dto = (User)SendAndAwaitResponse(clientSocket, branchEP, payload);
                return dto;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Login failed:\n {ex.Message}");
                return new User();
            }
        }
    }
}
