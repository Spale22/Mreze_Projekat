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
                var client = new Client
                {
                    FirstName = $"Client{i}",
                    LastName = $"Client{i}",
                    Balance = 1000.0 + i * 100
                };
                clientRepository.CreateClient(client);
                for (int k = 0; k < 5; k++)
                {
                    var transactionW = new Transaction
                    {
                        SenderId = client.ClientId,
                        RecipientId = client.ClientId,
                        Amount = -100.0,
                        Timestamp = DateTime.Now,
                        Type = TransactionType.Withdrawal
                    };
                    transactionRepository.CreateTransaction(transactionW);

                    var transactionD = new Transaction
                    {
                        SenderId = client.ClientId,
                        RecipientId = client.ClientId,
                        Amount = k * 100.0,
                        Timestamp = DateTime.Now,
                        Type = TransactionType.Deposit
                    };
                    transactionRepository.CreateTransaction(transactionD);
                }
            }
        }
    }
}
