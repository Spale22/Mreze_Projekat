using System;

namespace Domain
{
    [Serializable]
    public class Transaction
    {
        public Guid TransactionId { get; private set; } = Guid.Empty;
        public Guid SenderId { get; set; } = Guid.Empty;
        public Guid RecipientId { get; set; } = Guid.Empty;
        public double Amount { get; set; } = 0.0;
        public DateTime Timestamp { get; set; } = DateTime.MinValue;
        public TransactionType Type { get; set; } = TransactionType.Unknown;
        public Transaction() { }
        public Transaction(Guid senderClientId, Guid recipientClientId, double amount, TransactionType type)
        {
            TransactionId = Guid.NewGuid();
            SenderId = senderClientId;
            RecipientId = recipientClientId;
            Amount = amount;
            Timestamp = DateTime.UtcNow;
            Type = type;
        }
    }
}
