using Domain;
using Infrastructure;
using System;

namespace Server
{
    public class AuthenticationService
    {
        private readonly IUserRepository userRepository;
        public AuthenticationService(IUserRepository _userRepository)
        {
            userRepository = _userRepository;
        }
        public User Authenticate(AuthRequestDTO request)
        {
            var client = userRepository.GetByUsername(request.Username);
            if (client == null || client.Password != request.Password)
                throw new UnauthorizedAccessException("Invalid username or password.");
            return client;
        }
    }
}
