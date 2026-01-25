using Domain;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Infrastructure
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
                client.Balance = newBalance;
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
