using System;
using System.Collections.Generic;

namespace Domain
{
    public interface IClientRepository
    {
        bool CreateClient(User newClient);
        bool DeleteClient(Guid clientId);
        bool UpdateClient(Guid clientId, User updatedClient);
        User GetClientById(Guid clientId);
        IEnumerable<User> GetAllClients();
        bool UpdateClientBalance(Guid clientId, double newBalance);
    }
}
