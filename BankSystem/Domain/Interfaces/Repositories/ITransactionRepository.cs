using Domain.Models;
using System;
using System.Collections.Generic;

namespace Domain.Interfaces.Repositories
{
    public interface ITransactionRepository
    {
        bool CreateTransaction(Transaction newTransaction);
        Transaction GetTransactionById(Guid transactionId);
        IEnumerable<Transaction> GetAllTransactions();
    }
}
