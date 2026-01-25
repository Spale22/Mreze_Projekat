using Domain.DTOs;

namespace Domain.Interfaces.Services
{
    public interface IAuthenticationService
    {
        AuthResponseDTO Authenticate(AuthRequestDTO request);
    }
}
