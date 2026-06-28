using EventosVivos.Application.Abstractions;

namespace EventosVivos.Infrastructure;

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
