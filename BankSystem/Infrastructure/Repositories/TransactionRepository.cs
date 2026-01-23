using Domain.Enumerations;
using Domain.Interfaces.Repositories;
using Domain.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Infrastructure.Repositories
{
    public class TransactionRepository : ITransactionRepository
    {
        private readonly IClientRepository clientRepository;

        ConcurrentDictionary<Guid, Transaction> transactions = new ConcurrentDictionary<Guid, Transaction>();
        public TransactionRepository(IClientRepository _clientRepository)
        {
            clientRepository = _clientRepository;
        }
        public bool CreateTransaction(Transaction newTransaction)
        {
            switch (newTransaction.Type)
            {
                case TransactionType.Deposit:

                    var client = clientRepository.GetClientById(newTransaction.RecipientId);

                    if (client == null)
                        return false;

                    client.Balance += newTransaction.Amount;

                    if (!clientRepository.UpdateClientBalance(client.ClientId, client.Balance))
                        return false;

                    break;

                case TransactionType.Withdrawal:

                    var withdrawClient = clientRepository.GetClientById(newTransaction.SenderId);

                    if (withdrawClient == null || withdrawClient.Balance < newTransaction.Amount)
                        return false;

                    withdrawClient.Balance -= newTransaction.Amount;

                    if (!clientRepository.UpdateClientBalance(withdrawClient.ClientId, withdrawClient.Balance))
                        return false;

                    break;

                case TransactionType.Transfer:

                    var senderClient = clientRepository.GetClientById(newTransaction.SenderId);
                    var recipientClient = clientRepository.GetClientById(newTransaction.RecipientId);

                    if (senderClient == null || recipientClient == null || senderClient.Balance < newTransaction.Amount)
                        return false;

                    senderClient.Balance -= newTransaction.Amount;
                    recipientClient.Balance += newTransaction.Amount;

                    if (!clientRepository.UpdateClientBalance(senderClient.ClientId, senderClient.Balance))
                        return false;

                    if (!clientRepository.UpdateClientBalance(recipientClient.ClientId, recipientClient.Balance))
                        return false;

                    break;

                default:
                    return false;
            }

            return transactions.TryAdd(newTransaction.TransactionId, newTransaction);
        }

        public IEnumerable<Transaction> GetAllTransactions()
            => transactions.Values;

        public Transaction GetTransactionById(Guid transactionId)
            => transactions.TryGetValue(transactionId, out var transaction) ? transaction : null;

    }
}
