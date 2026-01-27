using System;

namespace Domain
{
    [Serializable]
    public class User
    {
        public Guid UserId { get; private set; } = Guid.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public double Balance { get; set; } = 0.0;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string AccountNumber { get; set; } = string.Empty;
        public User() { }
        public User(string username, string password, string firstName, string lastName, double initialBalance, string accountNumber = "")
        {
            UserId = Guid.NewGuid();
            FirstName = firstName;
            LastName = lastName;
            Balance = initialBalance;
            Username = username;
            Password = password;
            AccountNumber = accountNumber;
        }
    }
}
