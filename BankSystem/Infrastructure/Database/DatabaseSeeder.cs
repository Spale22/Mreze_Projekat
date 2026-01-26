using Domain;
using System;

namespace Infrastructure
{
    public static class DatabaseSeeder
    {
        public static void Seed(IClientRepository clientRepository, ITransactionRepository transactionRepository)
        {
            for (int i = 0; i < 5; i++)
            {
                var client = new User($"client{i}", $"password{i}", $"Client{i}", $"Client{i}", 1000.0 + i * 100, $"{i}");
                clientRepository.Create(client);

                for (int k = 0; k < 5; k++)
                {
                    var transactionW = new Transaction(client.UserId, 100, DateTime.Now, TransactionType.Withdraw);
                   
                    transactionRepository.Create(transactionW);

                    var transactionD = new Transaction(client.UserId,k*100,DateTime.Now, TransactionType.Deposit);
                   
                    transactionRepository.Create(transactionD);
                }
            }
        }
    }
}
