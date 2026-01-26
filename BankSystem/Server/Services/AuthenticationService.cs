using Domain;
using Infrastructure;
using System;

namespace Server
{
    public class AuthenticationService
    {
        private readonly IClientRepository clientRepository;
        public AuthenticationService(IClientRepository _clientRepository)
        {
            clientRepository = _clientRepository;
        }
        public User Authenticate(AuthRequestDTO request)
        {
            var client = clientRepository.GetByUsername(request.Username);
            if (client == null || client.Password != request.Password)
                throw new UnauthorizedAccessException("Invalid username or password.");
            return client;
        }
    }
}
