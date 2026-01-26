using System;
using System.Collections.Generic;

namespace Domain
{
    public interface ITransactionRepository
    {
        bool Create(Transaction newTransaction);
        Transaction GetTransactionById(Guid transactionId);
        IEnumerable<Transaction> GetAllTransactions();
    }
}
