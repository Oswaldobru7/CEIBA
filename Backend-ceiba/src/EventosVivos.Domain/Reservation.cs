namespace EventosVivos.Domain;

public sealed class Reservation
{
    public int Id { get; set; }
    public int EventId { get; set; }
    public int Quantity { get; set; }
    public required string BuyerName { get; set; }
    public required string BuyerEmail { get; set; }
    public ReservationStatus Status { get; set; } = ReservationStatus.PendientePago;
    public string? ReservationCode { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ConfirmedAt { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
    public int LostTickets { get; set; }
}
