using EventosVivos.Application.Abstractions;
using EventosVivos.Application.Common;
using EventosVivos.Domain;

namespace EventosVivos.Application.Events;

public sealed class EventService
{
    private readonly IClock _clock;
    private readonly IEventRepository _events;
    private readonly IVenueRepository _venues;

    public EventService(IClock clock, IEventRepository events, IVenueRepository venues)
    {
        _clock = clock;
        _events = events;
        _venues = venues;
    }

    public async Task<Result<EventResponse>> CreateAsync(CreateEventRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await ValidateCreateAsync(request, cancellationToken);
        if (validation is not null)
        {
            return Result<EventResponse>.Failure(validation);
        }

        EnumParsing.TryParseEventType(request.Type, out var type);
        var eventItem = new Event
        {
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            VenueId = request.VenueId,
            MaxCapacity = request.MaxCapacity,
            StartAt = request.StartAt,
            EndAt = request.EndAt,
            Price = request.Price,
            Type = type,
            Status = EventStatus.Activo
        };

        var created = await _events.AddAsync(eventItem, cancellationToken);
        return Result<EventResponse>.Success(created.ToResponse(), 201);
    }

    public async Task<Result<IReadOnlyList<EventResponse>>> ListAsync(EventQuery query, CancellationToken cancellationToken = default)
    {
        EventType? type = null;
        EventStatus? status = null;

        if (!string.IsNullOrWhiteSpace(query.Type))
        {
            if (!EnumParsing.TryParseEventType(query.Type, out var parsedType))
            {
                return Result<IReadOnlyList<EventResponse>>.Failure("Tipo de evento invalido.");
            }

            type = parsedType;
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            if (!EnumParsing.TryParseEventStatus(query.Status, out var parsedStatus))
            {
                return Result<IReadOnlyList<EventResponse>>.Failure("Estado de evento invalido.");
            }

            status = parsedStatus;
        }

        if (query.From.HasValue && query.To.HasValue && query.To < query.From)
        {
            return Result<IReadOnlyList<EventResponse>>.Failure("El rango de fechas es invalido.");
        }

        var events = await _events.ListAsync(cancellationToken);
        foreach (var eventItem in events)
        {
            eventItem.RefreshStatus(_clock.UtcNow);
            await _events.UpdateAsync(eventItem, cancellationToken);
        }

        var filtered = events.AsEnumerable();

        if (type.HasValue)
        {
            filtered = filtered.Where(e => e.Type == type.Value);
        }

        if (query.From.HasValue)
        {
            filtered = filtered.Where(e => e.StartAt >= query.From.Value);
        }

        if (query.To.HasValue)
        {
            filtered = filtered.Where(e => e.StartAt <= query.To.Value);
        }

        if (query.VenueId.HasValue)
        {
            filtered = filtered.Where(e => e.VenueId == query.VenueId.Value);
        }

        if (status.HasValue)
        {
            filtered = filtered.Where(e => e.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            filtered = filtered.Where(e => e.Title.Contains(query.Search.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        return Result<IReadOnlyList<EventResponse>>.Success(
            filtered.OrderBy(e => e.StartAt).Select(e => e.ToResponse()).ToArray());
    }

    private async Task<string?> ValidateCreateAsync(CreateEventRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Title) || request.Title.Trim().Length is < 5 or > 100)
        {
            return "El titulo es obligatorio y debe tener entre 5 y 100 caracteres.";
        }

        if (string.IsNullOrWhiteSpace(request.Description) || request.Description.Trim().Length is < 10 or > 500)
        {
            return "La descripcion es obligatoria y debe tener entre 10 y 500 caracteres.";
        }

        var venue = await _venues.GetByIdAsync(request.VenueId, cancellationToken);
        if (venue is null)
        {
            return "El venue no existe.";
        }

        if (request.MaxCapacity <= 0)
        {
            return "La capacidad maxima debe ser un entero positivo.";
        }

        if (request.MaxCapacity > venue.Capacity)
        {
            return "La capacidad del evento no puede exceder la capacidad del venue.";
        }

        if (request.StartAt <= _clock.UtcNow)
        {
            return "La fecha de inicio debe ser futura.";
        }

        if (request.EndAt <= request.StartAt)
        {
            return "La fecha de fin debe ser posterior al inicio.";
        }

        if (request.Price <= 0)
        {
            return "El precio de entrada debe ser positivo.";
        }

        if (!EnumParsing.TryParseEventType(request.Type, out _))
        {
            return "Tipo de evento invalido. Valores: conferencia, taller, concierto.";
        }

        if (IsWeekend(request.StartAt) && request.StartAt.TimeOfDay > new TimeSpan(22, 0, 0))
        {
            return "Los eventos en fin de semana no pueden iniciar despues de las 22:00.";
        }

        var existingEvents = await _events.ListAsync(cancellationToken);
        var hasOverlap = existingEvents.Any(existing =>
            existing.Status == EventStatus.Activo &&
            existing.VenueId == request.VenueId &&
            request.StartAt < existing.EndAt &&
            request.EndAt > existing.StartAt);

        return hasOverlap
            ? "Ya existe un evento activo en el mismo venue con horario superpuesto."
            : null;
    }

    private static bool IsWeekend(DateTimeOffset date) =>
        date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
}
