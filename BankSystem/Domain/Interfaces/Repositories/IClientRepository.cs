using System;
using System.Collections.Generic;

namespace Domain
{
    public interface IClientRepository
    {
        bool Create(User newClient);
        bool Delete(Guid clientId);
        bool Update(Guid clientId, User updatedClient);
        User GetClientById(Guid clientId);
        User GetByUsername(string username);
        double GetClientBalance(Guid clientId);
        IEnumerable<User> GetAllClients();
        bool UpdateClientBalance(Guid clientId, double newBalance);
        User GetClientByAccountNumber(string accountNumber);
    }
}
