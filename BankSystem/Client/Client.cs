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
        private static string enc_key = "";
        static void Main(string[] args)
        {
            Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            clientSocket.Blocking = false;

            IPEndPoint branchEP = new IPEndPoint(IPAddress.Loopback, 16001);

            var cts = new CancellationTokenSource();

            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
                Console.WriteLine("Shutdown requested (Ctrl+C).");
            };

            try
            {
                Console.WriteLine("UDP Client started.");
                Console.WriteLine("-----------------------------");
                Console.WriteLine("Press Ctrl+C to exit.");
                Console.WriteLine("-----------------------------");

                User currentUser = new User();

                GetEncKey(clientSocket, branchEP);

                while (!cts.IsCancellationRequested)
                {
                    while (currentUser.UserId == Guid.Empty && !cts.IsCancellationRequested)
                        currentUser = UserLogin(clientSocket, branchEP, ref cts);

                    if (cts.IsCancellationRequested)
                        break;

                    int opr = -1;
                    while ((opr < 0 || opr > 4) && !cts.IsCancellationRequested)
                    {
                        Console.WriteLine("Operation: ");
                        Console.WriteLine(" 0 - Logout");
                        Console.WriteLine(" 1 - Deposit");
                        Console.WriteLine(" 2 - Withdraw");
                        Console.WriteLine(" 3 - Transfer money");
                        Console.WriteLine(" 4 - Balance inquiry");
                        Console.WriteLine("-----------------------------");
                        Console.Write("Select operation (1-4): ");
                        Int32.TryParse(Console.ReadLine(), out opr);
                        Console.WriteLine("-----------------------------");
                    } 

                    if (cts.IsCancellationRequested)
                        break;

                    if (opr == 0)
                    {
                        Console.WriteLine("Logging out ...");
                        currentUser = new User();
                    }
                    else if (opr >= 1 && opr <= 3)
                        HandleTransaction(clientSocket, branchEP, ref currentUser, (TransactionType)opr);
                    else
                        HandleBalanceInquiry(clientSocket, branchEP, ref currentUser);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred:\n {ex.Message}");
            }
            finally
            {
                clientSocket.Close();
                Console.WriteLine("Client shutting down ...");
                Console.ReadKey();
            }
        }

        private static void GetEncKey(Socket clientSocket, IPEndPoint branchEP)
        {
            clientSocket.SendTo(new byte[] { 0x00 }, branchEP);
            byte[] key_buffer = new byte[37];
            EndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
            int timeoutMicros = 60_000_000;
            if (clientSocket.Poll(timeoutMicros, SelectMode.SelectRead))
            {
                int recievedBytes = clientSocket.ReceiveFrom(key_buffer, ref remoteEP);
                if (recievedBytes == 0)
                    throw new Exception("Failed to receive encryption key from server.");
                enc_key = System.Text.Encoding.UTF8.GetString(key_buffer, 0, recievedBytes);
                Console.WriteLine("Received encryption key from branch office");
            }
            else
                throw new Exception("No response within timeout.");
        }

        private static Object SendAndAwaitResponse(Socket clientSocket, IPEndPoint branchEP, byte[] payload)
        {
            byte[] encryptedPayload = Encryptor.Encrypt(enc_key, payload);
            if (encryptedPayload == null || encryptedPayload.Length == 0)
                throw new Exception("Encryption failed or resulted in empty data.");

            clientSocket.SendTo(encryptedPayload, branchEP);
            Object result = null;

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
                byte[] data = Encryptor.Decrypt(enc_key, frame);

                if (data == null || data.Length == 0)
                    throw new Exception("Decryption failed or resulted in empty data.");

                result = SerializationHelper.Deserialize<Object>(data);
                if (result.GetType() == typeof(string))
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
            byte[] payload = SerializationHelper.Serialize(currentUser.UserId);
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
        private static void HandleTransaction(Socket clientSocket, IPEndPoint branchEP, ref User currentUser, TransactionType transactionType)
        {
            Console.WriteLine($"{transactionType.ToString()} Operation");
            Console.WriteLine("-----------------------------");

            double amount = -1;
            while (amount <= 0)
            {
                Console.Write("Input amount: ");
                amount = Double.Parse(Console.ReadLine() ?? "");
                if (amount > 0)
                    break;
                Console.WriteLine("Amount must be greater than zero. Please try again.");
            }

            Transaction t;

            if (transactionType == TransactionType.Transfer)
            {
                Console.Write("Input recipient account number: ");
                string recipientAccountNumber = Console.ReadLine() ?? "";
                t = new Transaction(currentUser.UserId, amount, DateTime.Now, TransactionType.Transfer, recipientAccountNumber);
            }
            else
                t = new Transaction(currentUser.UserId, amount, DateTime.Now, transactionType);

            byte[] payload = SerializationHelper.Serialize(t);

            try
            {
                bool result = (bool)SendAndAwaitResponse(clientSocket, branchEP, payload);
                if (result)
                    Console.WriteLine($"{transactionType.ToString()}  successful.");
                else
                    Console.WriteLine($"{transactionType.ToString()}  failed.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{transactionType.ToString()}  failed:\n {ex.Message}");
            }
        }
        private static User UserLogin(Socket clientSocket, IPEndPoint branchEP, ref CancellationTokenSource cts)
        {
            Console.WriteLine("User Login");
            Console.WriteLine("-----------------------------");
            Console.Write("Enter username: ");
            string username = Console.ReadLine();

            if (cts.IsCancellationRequested)
                return new User();

            Console.Write("Enter password: ");
            string password = Console.ReadLine();

            if (cts.IsCancellationRequested)
                return new User();

            AuthRequestDTO authRequest = new AuthRequestDTO
            {
                Username = username,
                Password = password
            };
            byte[] payload = SerializationHelper.Serialize(authRequest);
            try
            {
                User dto = (User)SendAndAwaitResponse(clientSocket, branchEP, payload);

                Console.WriteLine("-----------------------------");
                Console.WriteLine($"Welcome, {dto.FirstName} {dto.LastName}!");
                Console.WriteLine("-----------------------------");
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
