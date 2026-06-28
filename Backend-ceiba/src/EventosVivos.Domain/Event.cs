namespace EventosVivos.Domain;

public sealed class Event
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public required string Description { get; set; }
    public int VenueId { get; set; }
    public int MaxCapacity { get; set; }
    public DateTimeOffset StartAt { get; set; }
    public DateTimeOffset EndAt { get; set; }
    public decimal Price { get; set; }
    public EventType Type { get; set; }
    public EventStatus Status { get; set; } = EventStatus.Activo;

    public void RefreshStatus(DateTimeOffset now)
    {
        if (Status == EventStatus.Activo && now > EndAt)
        {
            Status = EventStatus.Completado;
        }
    }
}
