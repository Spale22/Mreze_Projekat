using Domain.DTOs;

namespace Domain.Interfaces.Services
{
    public interface IClientDataService
    {
        ClientDataResponseDTO GetClientData(ClientDataRequestDTO request);
    }
}
