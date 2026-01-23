using Domain.Interfaces.Repositories;
using Domain.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Infrastructure.Repositories
{
    public class ClientRepository : IClientRepository
    {
        ConcurrentDictionary<Guid, Client> clients = new ConcurrentDictionary<Guid, Client>();
        public bool CreateClient(Client newClient)
            => clients.TryAdd(newClient.ClientId, newClient);

        public bool DeleteClient(Guid clientId)
            => clients.TryRemove(clientId, out _);

        public IEnumerable<Client> GetAllClients()
            => clients.Values;

        public Client GetClientById(Guid clientId)
            => clients.TryGetValue(clientId, out var client) ? client : null;

        public bool UpdateClient(Guid clientId, Client updatedClient)
            => clients.TryUpdate(clientId, updatedClient, clients[clientId]);

        public bool UpdateClientBalance(Guid clientId, double newBalance)
        {
            if (clients.TryGetValue(clientId, out var client))
            {
                Client updatedClient = new Client
                {
                    ClientId = client.ClientId,
                    FirstName = client.FirstName,
                    LastName = client.LastName,
                    Balance = newBalance
                };
                return clients.TryUpdate(clientId, updatedClient, client);
            }
            else
            {
                return false;
            }
        }
    }
}
