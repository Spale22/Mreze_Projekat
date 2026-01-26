using System;

namespace Domain
{
    [Serializable]
    public class Transaction
    {
        public Guid TransactionId { get; private set; } = Guid.Empty;
        public Guid SenderId { get; set; } = Guid.Empty;
        public string RecipientAccountNumber { get; set; } = string.Empty;
        public double Amount { get; set; } = 0.0;
        public DateTime Timestamp { get; set; } = DateTime.MinValue;
        public TransactionType Type { get; set; } = TransactionType.Unknown;
        public Transaction() { }
        public Transaction(Guid senderClientId, double amount, TransactionType type, string accountNumber = "")
        {
            TransactionId = Guid.NewGuid();
            SenderId = senderClientId;
            RecipientAccountNumber = accountNumber;
            Amount = amount;
            Timestamp = DateTime.UtcNow;
            Type = type;
        }
    }
}
