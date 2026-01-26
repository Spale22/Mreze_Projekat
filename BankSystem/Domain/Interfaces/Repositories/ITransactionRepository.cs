using System;
using System.Collections.Generic;

namespace Domain
{
    public interface ITransactionRepository
    {
        bool CreateTransaction(Transaction newTransaction);
        Transaction GetTransactionById(Guid transactionId);
        IEnumerable<Transaction> GetAllTransactions();
    }
}
