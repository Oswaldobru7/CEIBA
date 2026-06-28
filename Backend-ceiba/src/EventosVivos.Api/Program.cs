using EventosVivos.Application.Abstractions;
using EventosVivos.Application.Common;
using EventosVivos.Application.Events;
using EventosVivos.Application.Reservations;
using EventosVivos.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyHeader()
            .AllowAnyMethod()
            .WithOrigins("http://localhost:4200"));
});

builder.Services.AddInfrastructure();
builder.Services.AddScoped<EventService>();
builder.Services.AddScoped<ReservationService>();

var app = builder.Build();

app.UseCors();

app.MapGet("/", () => Results.Ok(new
{
    name = "EventosVivos API",
    endpoints = new[]
    {
        "GET /api/venues",
        "POST /api/events",
        "GET /api/events",
        "POST /api/reservations",
        "POST /api/reservations/{id}/confirm-payment",
        "POST /api/reservations/{id}/cancel",
        "GET /api/events/{id}/occupancy-report"
    }
}));

app.MapGet("/api/venues", async (IVenueRepository venues, CancellationToken cancellationToken) =>
{
    var items = await venues.ListAsync(cancellationToken);
    return Results.Ok(items);
});

app.MapPost("/api/events", async (CreateEventRequest request, EventService service, CancellationToken cancellationToken) =>
{
    var result = await service.CreateAsync(request, cancellationToken);
    return ToHttpResult(result, value => Results.Created($"/api/events/{value.Id}", value));
});

app.MapGet("/api/events", async (
    string? type,
    DateTimeOffset? from,
    DateTimeOffset? to,
    int? venueId,
    string? status,
    string? search,
    EventService service,
    CancellationToken cancellationToken) =>
{
    var result = await service.ListAsync(new EventQuery(type, from, to, venueId, status, search), cancellationToken);
    return ToHttpResult(result);
});

app.MapPost("/api/reservations", async (CreateReservationRequest request, ReservationService service, CancellationToken cancellationToken) =>
{
    var result = await service.CreateAsync(request, cancellationToken);
    return ToHttpResult(result, value => Results.Created($"/api/reservations/{value.Id}", value));
});

app.MapPost("/api/reservations/{id:int}/confirm-payment", async (int id, ReservationService service, CancellationToken cancellationToken) =>
{
    var result = await service.ConfirmPaymentAsync(id, cancellationToken);
    return ToHttpResult(result);
});

app.MapPost("/api/reservations/{id:int}/cancel", async (int id, ReservationService service, CancellationToken cancellationToken) =>
{
    var result = await service.CancelAsync(id, cancellationToken);
    return ToHttpResult(result);
});

app.MapGet("/api/events/{id:int}/occupancy-report", async (int id, ReservationService service, CancellationToken cancellationToken) =>
{
    var result = await service.GetOccupancyReportAsync(id, cancellationToken);
    return ToHttpResult(result);
});

app.Run();

static IResult ToHttpResult<T>(Result<T> result, Func<T, IResult>? successFactory = null)
{
    if (result.IsSuccess && result.Value is not null)
    {
        return successFactory is null ? Results.Ok(result.Value) : successFactory(result.Value);
    }

    return Results.Problem(
        title: result.StatusCode == 404 ? "Recurso no encontrado" : "Solicitud invalida",
        detail: result.Error,
        statusCode: result.StatusCode);
}
