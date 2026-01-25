using System;

namespace Domain
{
    [Serializable]
    public class Client
    {
        public Guid ClientId { get; private set; } = Guid.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public double Balance { get; set; } = 0.0;
        public Client() { }
        public Client(string firstName, string lastName, double initialBalance)
        {
            ClientId = Guid.NewGuid();
            FirstName = firstName;
            LastName = lastName;
            Balance = initialBalance;
        }
    }
}
