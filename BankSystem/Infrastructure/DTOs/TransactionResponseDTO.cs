using System;

namespace Infrastructure
{
    [Serializable]
    public class TransactionResponseDTO
    {
        public bool Result { get; set; } = false;
    }
}
