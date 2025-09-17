using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using MantenimientoApi.Models;

namespace MantenimientoApi.Repositories
{
    public interface IMaintenanceRepository
    {
        Maintenance Save(Maintenance m);
        IEnumerable<Maintenance> List();
        Maintenance? GetById(Guid id);
    }

    public class InMemoryMaintenanceRepository : IMaintenanceRepository
    {
        private readonly ConcurrentDictionary<Guid, Maintenance> _store = new();

        public Maintenance Save(Maintenance m)
        {
            _store[m.Id] = m;
            return m;
        }

        public IEnumerable<Maintenance> List() => _store.Values.OrderByDescending(x => x.CreatedAt);

        public Maintenance? GetById(Guid id) => _store.TryGetValue(id, out var m) ? m : null;
    }
}
