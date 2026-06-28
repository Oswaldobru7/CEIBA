using EventosVivos.Domain;

namespace EventosVivos.Application.Abstractions;

public interface IVenueRepository
{
    Task<Venue?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Venue>> ListAsync(CancellationToken cancellationToken = default);
}
