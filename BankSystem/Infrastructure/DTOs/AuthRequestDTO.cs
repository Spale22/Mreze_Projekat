using System;

namespace Infrastructure
{
    [Serializable]
    public class AuthRequestDTO
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
