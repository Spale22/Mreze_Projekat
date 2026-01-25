using System;

namespace Infrastructure
{
    [Serializable]
    public class ClientDataRequestDTO
    {
        public string Operation { get; set; }
        public double Amount { get; set; }
    }
}
