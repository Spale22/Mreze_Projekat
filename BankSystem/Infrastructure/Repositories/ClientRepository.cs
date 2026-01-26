using Domain;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Infrastructure
{
    public class ClientRepository : IClientRepository
    {
        ConcurrentDictionary<Guid, User> clients = new ConcurrentDictionary<Guid, User>();
        public bool Create(User newClient)
            => clients.TryAdd(newClient.UserId, newClient);

        public bool Delete(Guid clientId)
            => clients.TryRemove(clientId, out _);

        public IEnumerable<User> GetAllClients()
            => clients.Values;

        public User GetClientByAccountNumber(string accountNumber)
            => clients.Values.FirstOrDefault(c => c.AccountNumber == accountNumber);

        public User GetClientById(Guid clientId)
            => clients.TryGetValue(clientId, out var client) ? client : null;

        public bool Update(Guid clientId, User updatedClient)
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
