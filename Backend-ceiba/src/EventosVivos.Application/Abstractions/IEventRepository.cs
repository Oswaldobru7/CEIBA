using EventosVivos.Domain;

namespace EventosVivos.Application.Abstractions;

public interface IEventRepository
{
    Task<Event?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Event>> ListAsync(CancellationToken cancellationToken = default);
    Task<Event> AddAsync(Event eventItem, CancellationToken cancellationToken = default);
    Task UpdateAsync(Event eventItem, CancellationToken cancellationToken = default);
}
