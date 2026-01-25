using System;

namespace Domain.DTOs
{
    [Serializable]
    public class ClientDataResponseDTO
    {
        public bool Success { get; set; }
        public string Message { get; set; }
    }
}
