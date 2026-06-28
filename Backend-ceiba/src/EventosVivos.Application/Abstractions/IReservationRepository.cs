using EventosVivos.Domain;

namespace EventosVivos.Application.Abstractions;

public interface IReservationRepository
{
    Task<Reservation?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Reservation>> ListByEventAsync(int eventId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Reservation>> ListAsync(CancellationToken cancellationToken = default);
    Task<Reservation> AddAsync(Reservation reservation, CancellationToken cancellationToken = default);
    Task UpdateAsync(Reservation reservation, CancellationToken cancellationToken = default);
    Task<bool> ReservationCodeExistsAsync(string code, CancellationToken cancellationToken = default);
}
