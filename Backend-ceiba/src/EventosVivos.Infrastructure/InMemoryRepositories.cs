using EventosVivos.Application.Abstractions;
using EventosVivos.Domain;

namespace EventosVivos.Infrastructure;

internal sealed class InMemoryEventRepository : IEventRepository
{
    private readonly InMemoryStore _store;

    public InMemoryEventRepository(InMemoryStore store) => _store = store;

    public Task<Event?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        lock (_store.SyncRoot)
        {
            return Task.FromResult(_store.Events.FirstOrDefault(e => e.Id == id));
        }
    }

    public Task<IReadOnlyList<Event>> ListAsync(CancellationToken cancellationToken = default)
    {
        lock (_store.SyncRoot)
        {
            return Task.FromResult<IReadOnlyList<Event>>(_store.Events.ToArray());
        }
    }

    public Task<Event> AddAsync(Event eventItem, CancellationToken cancellationToken = default)
    {
        lock (_store.SyncRoot)
        {
            eventItem.Id = _store.NextEventId++;
            _store.Events.Add(eventItem);
            return Task.FromResult(eventItem);
        }
    }

    public Task UpdateAsync(Event eventItem, CancellationToken cancellationToken = default) => Task.CompletedTask;
}

internal sealed class InMemoryReservationRepository : IReservationRepository
{
    private readonly InMemoryStore _store;

    public InMemoryReservationRepository(InMemoryStore store) => _store = store;

    public Task<Reservation?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        lock (_store.SyncRoot)
        {
            return Task.FromResult(_store.Reservations.FirstOrDefault(r => r.Id == id));
        }
    }

    public Task<IReadOnlyList<Reservation>> ListByEventAsync(int eventId, CancellationToken cancellationToken = default)
    {
        lock (_store.SyncRoot)
        {
            return Task.FromResult<IReadOnlyList<Reservation>>(_store.Reservations.Where(r => r.EventId == eventId).ToArray());
        }
    }

    public Task<IReadOnlyList<Reservation>> ListAsync(CancellationToken cancellationToken = default)
    {
        lock (_store.SyncRoot)
        {
            return Task.FromResult<IReadOnlyList<Reservation>>(_store.Reservations.ToArray());
        }
    }

    public Task<Reservation> AddAsync(Reservation reservation, CancellationToken cancellationToken = default)
    {
        lock (_store.SyncRoot)
        {
            reservation.Id = _store.NextReservationId++;
            _store.Reservations.Add(reservation);
            return Task.FromResult(reservation);
        }
    }

    public Task UpdateAsync(Reservation reservation, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task<bool> ReservationCodeExistsAsync(string code, CancellationToken cancellationToken = default)
    {
        lock (_store.SyncRoot)
        {
            return Task.FromResult(_store.Reservations.Any(r => r.ReservationCode == code));
        }
    }
}

internal sealed class InMemoryVenueRepository : IVenueRepository
{
    private readonly InMemoryStore _store;

    public InMemoryVenueRepository(InMemoryStore store) => _store = store;

    public Task<Venue?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        lock (_store.SyncRoot)
        {
            return Task.FromResult(_store.Venues.FirstOrDefault(v => v.Id == id));
        }
    }

    public Task<IReadOnlyList<Venue>> ListAsync(CancellationToken cancellationToken = default)
    {
        lock (_store.SyncRoot)
        {
            return Task.FromResult<IReadOnlyList<Venue>>(_store.Venues.ToArray());
        }
    }
}
