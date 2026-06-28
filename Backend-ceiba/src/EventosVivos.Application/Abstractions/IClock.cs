namespace EventosVivos.Application.Abstractions;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
