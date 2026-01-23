using Domain.Models;
using System;
using System.Collections.Generic;

namespace Domain.Interfaces.Repositories
{
    public interface IClientRepository
    {
        bool CreateClient(Client newClient);
        bool DeleteClient(Guid clientId);
        bool UpdateClient(Guid clientId, Client updatedClient);
        Client GetClientById(Guid clientId);
        IEnumerable<Client> GetAllClients();
        bool UpdateClientBalance(Guid clientId, double newBalance);
    }
}
