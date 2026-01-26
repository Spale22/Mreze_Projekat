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
                var client = new User
                {
                    FirstName = $"Client{i}",
                    LastName = $"Client{i}",
                    Username = $"client{i}",
                    Password = $"password{i}",
                    AccountNumber = $"{i}",
                    Balance = 1000.0 + i * 100
                };
                clientRepository.Create(client);
                for (int k = 0; k < 5; k++)
                {
                    var transactionW = new Transaction
                    {
                        SenderId = client.UserId,
                        Amount = -100.0,
                        Timestamp = DateTime.Now,
                        Type = TransactionType.Withdraw
                    };
                    transactionRepository.Create(transactionW);

                    var transactionD = new Transaction
                    {
                        SenderId = client.UserId,
                        Amount = k * 100.0,
                        Timestamp = DateTime.Now,
                        Type = TransactionType.Deposit
                    };
                    transactionRepository.Create(transactionD);
                }
            }
        }
    }
}
