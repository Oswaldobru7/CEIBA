using EventosVivos.Domain;

namespace EventosVivos.Application.Common;

public static class EnumParsing
{
    public static bool TryParseEventType(string? value, out EventType eventType)
    {
        eventType = default;
        return Normalize(value) switch
        {
            "conferencia" => Set(EventType.Conferencia, out eventType),
            "taller" => Set(EventType.Taller, out eventType),
            "concierto" => Set(EventType.Concierto, out eventType),
            _ => false
        };
    }

    public static bool TryParseEventStatus(string? value, out EventStatus status)
    {
        status = default;
        return Normalize(value) switch
        {
            "activo" => Set(EventStatus.Activo, out status),
            "cancelado" => Set(EventStatus.Cancelado, out status),
            "completado" => Set(EventStatus.Completado, out status),
            _ => false
        };
    }

    public static string ToApiValue(this EventType eventType) =>
        eventType switch
        {
            EventType.Conferencia => "conferencia",
            EventType.Taller => "taller",
            EventType.Concierto => "concierto",
            _ => eventType.ToString()
        };

    public static string ToApiValue(this EventStatus status) =>
        status switch
        {
            EventStatus.Activo => "activo",
            EventStatus.Cancelado => "cancelado",
            EventStatus.Completado => "completado",
            _ => status.ToString()
        };

    public static string ToApiValue(this ReservationStatus status) =>
        status switch
        {
            ReservationStatus.PendientePago => "pendiente_pago",
            ReservationStatus.Confirmada => "confirmada",
            ReservationStatus.Cancelada => "cancelada",
            _ => status.ToString()
        };

    private static bool Set<T>(T value, out T target)
    {
        target = value;
        return true;
    }

    private static string Normalize(string? value) =>
        value?.Trim().ToLowerInvariant() ?? string.Empty;
}
