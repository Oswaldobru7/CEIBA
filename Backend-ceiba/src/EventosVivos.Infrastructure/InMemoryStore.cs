using EventosVivos.Domain;

namespace EventosVivos.Infrastructure;

internal sealed class InMemoryStore
{
    public object SyncRoot { get; } = new();
    public List<Event> Events { get; } = [];
    public List<Reservation> Reservations { get; } = [];
    public List<Venue> Venues { get; } =
    [
        new Venue(1, "Auditorio Central", 200, "Bogota"),
        new Venue(2, "Sala Norte", 50, "Bogota"),
        new Venue(3, "Arena Sur", 500, "Medellin")
    ];

    public int NextEventId { get; set; } = 1;
    public int NextReservationId { get; set; } = 1;
}
