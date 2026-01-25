using Domain.DTOs;

namespace Domain.Interfaces.Services
{
    public interface ITransactionService
    {
        TransactionResponseDTO ProcessTransaction(TransactionRequestDTO request);
    }
}
