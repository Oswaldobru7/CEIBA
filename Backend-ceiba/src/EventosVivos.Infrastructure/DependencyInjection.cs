using EventosVivos.Application.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace EventosVivos.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<InMemoryStore>();
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IEventRepository, InMemoryEventRepository>();
        services.AddSingleton<IReservationRepository, InMemoryReservationRepository>();
        services.AddSingleton<IVenueRepository, InMemoryVenueRepository>();
        return services;
    }
}
