using System;
using System.Net;
using System.Net.Sockets;

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
            clientSocket.ReceiveTimeout = 2000;

            IPEndPoint branchEP = new IPEndPoint(IPAddress.Loopback, 16001);


            var cts = new System.Threading.CancellationTokenSource();

            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
                Console.WriteLine("Shutdown requested (Ctrl+C).");
            };

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
                    Int32.TryParse(Console.ReadLine(), out opr);
                } while (opr < 0 || opr > 3);

                switch (opr)
                {
                    case 1:
                        HandleBalanceInqiry(clientSocket, branchEP);
                        break;
                    case 2:
                        HandleDeposit(clientSocket, branchEP);
                        break;
                    case 3:
                        HandleWithdraw(clientSocket, branchEP);
                        break;
                    case 4:
                        HandleTransfer(clientSocket, branchEP);
                        break;
                }
            }

            clientSocket.Close();
            Console.WriteLine("Client shuting down ...");
            Console.ReadKey();
        }

        private static void HandleBalanceInqiry(Socket clientSocket, IPEndPoint branchEP)
        {

        }
        private static void HandleDeposit(Socket clientSocket, IPEndPoint branchEP)
        {

        }
        private static void HandleWithdraw(Socket clientSocket, IPEndPoint branchEP)
        {

        }
        private static void HandleTransfer(Socket clientSocket, IPEndPoint branchEP)
        {

        }
    }
}
