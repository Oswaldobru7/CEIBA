using EventosVivos.Application.Common;

namespace EventosVivos.Application.Reservations;

public sealed record CreateReservationRequest(
    int EventId,
    int Quantity,
    string BuyerName,
    string BuyerEmail);

public sealed record ReservationResponse(
    int Id,
    int EventId,
    int Quantity,
    string BuyerName,
    string BuyerEmail,
    string Status,
    string? ReservationCode,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ConfirmedAt,
    DateTimeOffset? CancelledAt,
    int LostTickets);

public sealed record OccupancyReportResponse(
    int EventId,
    string EventTitle,
    int SoldTickets,
    int LostTickets,
    int AvailableTickets,
    decimal OccupancyPercentage,
    decimal Income,
    string Status);

public static class ReservationMappings
{
    public static ReservationResponse ToResponse(this Domain.Reservation reservation) =>
        new(
            reservation.Id,
            reservation.EventId,
            reservation.Quantity,
            reservation.BuyerName,
            reservation.BuyerEmail,
            reservation.Status.ToApiValue(),
            reservation.ReservationCode,
            reservation.CreatedAt,
            reservation.ConfirmedAt,
            reservation.CancelledAt,
            reservation.LostTickets);
}
