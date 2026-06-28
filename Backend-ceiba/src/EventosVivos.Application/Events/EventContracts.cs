using EventosVivos.Application.Common;
using EventosVivos.Domain;

namespace EventosVivos.Application.Events;

public sealed record CreateEventRequest(
    string Title,
    string Description,
    int VenueId,
    int MaxCapacity,
    DateTimeOffset StartAt,
    DateTimeOffset EndAt,
    decimal Price,
    string Type);

public sealed record EventQuery(
    string? Type,
    DateTimeOffset? From,
    DateTimeOffset? To,
    int? VenueId,
    string? Status,
    string? Search);

public sealed record EventResponse(
    int Id,
    string Title,
    string Description,
    int VenueId,
    int MaxCapacity,
    DateTimeOffset StartAt,
    DateTimeOffset EndAt,
    decimal Price,
    string Type,
    string Status);

public static class EventMappings
{
    public static EventResponse ToResponse(this Event eventItem) =>
        new(
            eventItem.Id,
            eventItem.Title,
            eventItem.Description,
            eventItem.VenueId,
            eventItem.MaxCapacity,
            eventItem.StartAt,
            eventItem.EndAt,
            eventItem.Price,
            eventItem.Type.ToApiValue(),
            eventItem.Status.ToApiValue());
}
