using System;
using Domain;

namespace Infrastructure
{
    [Serializable]
    public class AuthResponseDTO
    {
        public User LoggedClient { get; set; }
    }
}
