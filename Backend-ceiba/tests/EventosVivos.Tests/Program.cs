using EventosVivos.Application.Abstractions;
using EventosVivos.Application.Events;
using EventosVivos.Application.Reservations;
using EventosVivos.Domain;

var tests = new List<(string Name, Func<Task> Body)>
{
    ("Crear evento rechaza capacidad mayor al venue", CreateEventRejectsCapacityGreaterThanVenue),
    ("Crear evento rechaza superposicion de venue", CreateEventRejectsVenueOverlap),
    ("Crear evento permite horario contiguo sin superposicion", CreateEventAllowsAdjacentSchedule),
    ("Crear evento rechaza fin de semana despues de las 22:00", CreateEventRejectsWeekendAfterTenPm),
    ("Crear evento permite fin de semana exactamente a las 22:00", CreateEventAllowsWeekendAtTenPm),
    ("Reserva rechaza evento que inicia en menos de 1 hora", ReservationRejectsLessThanOneHour),
    ("Reserva aplica maximo 5 tickets si inicia en menos de 24 horas", ReservationLessThan24HoursOverridesPriceLimit),
    ("Reserva aplica maximo 10 tickets para precio mayor a 100", ReservationExpensiveEventLimitIsTen),
    ("Confirmar pago genera codigo EV de seis digitos", ConfirmPaymentGeneratesCode),
    ("Cancelar con menos de 48 horas marca entradas perdidas", CancelLateMarksLostTickets),
    ("Reporte calcula vendidos, perdidos, disponibles e ingresos", ReportCalculatesOccupancy)
};

var failures = 0;
foreach (var test in tests)
{
    try
    {
        await test.Body();
        Console.WriteLine($"PASS {test.Name}");
    }
    catch (Exception ex)
    {
        failures++;
        Console.WriteLine($"FAIL {test.Name}");
        Console.WriteLine($"     {ex.Message}");
    }
}

if (failures > 0)
{
    Console.WriteLine($"{failures} prueba(s) fallaron.");
    Environment.Exit(1);
}

Console.WriteLine($"{tests.Count} pruebas ejecutadas correctamente.");

static async Task CreateEventRejectsCapacityGreaterThanVenue()
{
    var ctx = TestContext.Create();
    var result = await ctx.Events.CreateAsync(ValidEvent(maxCapacity: 201));
    AssertFalse(result.IsSuccess, "El evento no debio crearse.");
}

static async Task CreateEventRejectsVenueOverlap()
{
    var ctx = TestContext.Create();
    var first = await ctx.Events.CreateAsync(ValidEvent());
    AssertTrue(first.IsSuccess, "El primer evento debio crearse.");

    var overlap = await ctx.Events.CreateAsync(ValidEvent(
        startAt: ctx.Clock.UtcNow.AddDays(2).AddHours(1),
        endAt: ctx.Clock.UtcNow.AddDays(2).AddHours(3)));

    AssertFalse(overlap.IsSuccess, "El evento superpuesto debio rechazarse.");
}

static async Task CreateEventAllowsAdjacentSchedule()
{
    var ctx = TestContext.Create();
    var first = await ctx.Events.CreateAsync(ValidEvent());
    AssertTrue(first.IsSuccess, "El primer evento debio crearse.");

    var adjacent = await ctx.Events.CreateAsync(ValidEvent(
        startAt: ctx.Clock.UtcNow.AddDays(2).AddHours(2),
        endAt: ctx.Clock.UtcNow.AddDays(2).AddHours(4)));

    AssertTrue(adjacent.IsSuccess, "Un evento que inicia al terminar otro no se superpone.");
}

static async Task CreateEventRejectsWeekendAfterTenPm()
{
    var ctx = TestContext.Create(new DateTimeOffset(2026, 6, 26, 12, 0, 0, TimeSpan.Zero));
    var result = await ctx.Events.CreateAsync(ValidEvent(
        startAt: new DateTimeOffset(2026, 6, 27, 22, 1, 0, TimeSpan.Zero),
        endAt: new DateTimeOffset(2026, 6, 27, 23, 0, 0, TimeSpan.Zero)));

    AssertFalse(result.IsSuccess, "Sabado despues de las 22:00 debe rechazarse.");
}

static async Task CreateEventAllowsWeekendAtTenPm()
{
    var ctx = TestContext.Create(new DateTimeOffset(2026, 6, 26, 12, 0, 0, TimeSpan.Zero));
    var result = await ctx.Events.CreateAsync(ValidEvent(
        startAt: new DateTimeOffset(2026, 6, 27, 22, 0, 0, TimeSpan.Zero),
        endAt: new DateTimeOffset(2026, 6, 27, 23, 0, 0, TimeSpan.Zero)));

    AssertTrue(result.IsSuccess, "Sabado a las 22:00 exactas debe permitirse.");
}

static async Task ReservationRejectsLessThanOneHour()
{
    var ctx = TestContext.Create();
    var created = await ctx.Events.CreateAsync(ValidEvent(
        startAt: ctx.Clock.UtcNow.AddMinutes(50),
        endAt: ctx.Clock.UtcNow.AddHours(2),
        venueId: 2,
        maxCapacity: 30));
    AssertTrue(created.IsSuccess, "El evento futuro debio crearse.");

    var reservation = await ctx.Reservations.CreateAsync(new CreateReservationRequest(created.Value!.Id, 1, "Ana", "ana@test.com"));
    AssertFalse(reservation.IsSuccess, "La reserva tardia debio rechazarse.");
}

static async Task ReservationLessThan24HoursOverridesPriceLimit()
{
    var ctx = TestContext.Create();
    var created = await ctx.Events.CreateAsync(ValidEvent(
        startAt: ctx.Clock.UtcNow.AddHours(20),
        endAt: ctx.Clock.UtcNow.AddHours(22),
        price: 150,
        venueId: 2,
        maxCapacity: 30));
    AssertTrue(created.IsSuccess, "El evento debio crearse.");

    var sixTickets = await ctx.Reservations.CreateAsync(new CreateReservationRequest(created.Value!.Id, 6, "Ana", "ana@test.com"));
    var fiveTickets = await ctx.Reservations.CreateAsync(new CreateReservationRequest(created.Value.Id, 5, "Ana", "ana@test.com"));

    AssertFalse(sixTickets.IsSuccess, "En menos de 24h el maximo debe ser 5.");
    AssertTrue(fiveTickets.IsSuccess, "Cinco entradas deben permitirse.");
}

static async Task ReservationExpensiveEventLimitIsTen()
{
    var ctx = TestContext.Create();
    var created = await ctx.Events.CreateAsync(ValidEvent(price: 120, venueId: 2, maxCapacity: 30));
    AssertTrue(created.IsSuccess, "El evento debio crearse.");

    var elevenTickets = await ctx.Reservations.CreateAsync(new CreateReservationRequest(created.Value!.Id, 11, "Ana", "ana@test.com"));
    var tenTickets = await ctx.Reservations.CreateAsync(new CreateReservationRequest(created.Value.Id, 10, "Ana", "ana@test.com"));

    AssertFalse(elevenTickets.IsSuccess, "Eventos caros deben rechazar mas de 10 entradas.");
    AssertTrue(tenTickets.IsSuccess, "Diez entradas deben permitirse.");
}

static async Task ConfirmPaymentGeneratesCode()
{
    var ctx = TestContext.Create();
    var created = await ctx.Events.CreateAsync(ValidEvent());
    var reservation = await ctx.Reservations.CreateAsync(new CreateReservationRequest(created.Value!.Id, 2, "Ana", "ana@test.com"));

    var confirmed = await ctx.Reservations.ConfirmPaymentAsync(reservation.Value!.Id);

    AssertTrue(confirmed.IsSuccess, "La confirmacion debio funcionar.");
    AssertTrue(confirmed.Value!.Status == "confirmada", "El estado debe ser confirmada.");
    AssertTrue(System.Text.RegularExpressions.Regex.IsMatch(confirmed.Value.ReservationCode!, "^EV-[0-9]{6}$"), "El codigo debe tener formato EV-######.");
}

static async Task CancelLateMarksLostTickets()
{
    var ctx = TestContext.Create();
    var created = await ctx.Events.CreateAsync(ValidEvent(
        startAt: ctx.Clock.UtcNow.AddHours(30),
        endAt: ctx.Clock.UtcNow.AddHours(32),
        venueId: 2,
        maxCapacity: 30));
    var reservation = await ctx.Reservations.CreateAsync(new CreateReservationRequest(created.Value!.Id, 3, "Ana", "ana@test.com"));
    var confirmed = await ctx.Reservations.ConfirmPaymentAsync(reservation.Value!.Id);

    var cancelled = await ctx.Reservations.CancelAsync(confirmed.Value!.Id);

    AssertTrue(cancelled.IsSuccess, "La cancelacion debio funcionar.");
    AssertTrue(cancelled.Value!.LostTickets == 3, "Las entradas deben marcarse como perdidas.");
}

static async Task ReportCalculatesOccupancy()
{
    var ctx = TestContext.Create();
    var created = await ctx.Events.CreateAsync(ValidEvent(
        startAt: ctx.Clock.UtcNow.AddHours(30),
        endAt: ctx.Clock.UtcNow.AddHours(32),
        venueId: 2,
        maxCapacity: 20,
        price: 50));
    var confirmedReservation = await ctx.Reservations.CreateAsync(new CreateReservationRequest(created.Value!.Id, 4, "Ana", "ana@test.com"));
    await ctx.Reservations.ConfirmPaymentAsync(confirmedReservation.Value!.Id);
    var cancelledReservation = await ctx.Reservations.CreateAsync(new CreateReservationRequest(created.Value.Id, 2, "Luis", "luis@test.com"));
    var confirmedToCancel = await ctx.Reservations.ConfirmPaymentAsync(cancelledReservation.Value!.Id);
    await ctx.Reservations.CancelAsync(confirmedToCancel.Value!.Id);

    var report = await ctx.Reservations.GetOccupancyReportAsync(created.Value.Id);

    AssertTrue(report.IsSuccess, "El reporte debio generarse.");
    AssertTrue(report.Value!.SoldTickets == 4, "Solo reservas confirmadas cuentan como vendidas.");
    AssertTrue(report.Value.LostTickets == 2, "Cancelacion tardia debe quedar como perdida.");
    AssertTrue(report.Value.AvailableTickets == 14, "Disponibles = capacidad - vendidas - perdidas.");
    AssertTrue(report.Value.Income == 200, "Ingresos = vendidas * precio.");
}

static CreateEventRequest ValidEvent(
    int venueId = 1,
    int maxCapacity = 100,
    DateTimeOffset? startAt = null,
    DateTimeOffset? endAt = null,
    decimal price = 80) =>
    new(
        "Conferencia IA",
        "Evento cultural con contenido suficiente",
        venueId,
        maxCapacity,
        startAt ?? TestContext.DefaultNow.AddDays(2),
        endAt ?? TestContext.DefaultNow.AddDays(2).AddHours(2),
        price,
        "conferencia");

static void AssertTrue(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

static void AssertFalse(bool condition, string message) => AssertTrue(!condition, message);

internal sealed class TestContext
{
    public static readonly DateTimeOffset DefaultNow = new(2026, 6, 27, 12, 0, 0, TimeSpan.Zero);

    private TestContext(FakeClock clock, InMemoryEventRepository eventsRepo, InMemoryReservationRepository reservationsRepo, InMemoryVenueRepository venuesRepo)
    {
        Clock = clock;
        Events = new EventService(clock, eventsRepo, venuesRepo);
        Reservations = new ReservationService(clock, eventsRepo, reservationsRepo);
    }

    public FakeClock Clock { get; }
    public EventService Events { get; }
    public ReservationService Reservations { get; }

    public static TestContext Create(DateTimeOffset? now = null)
    {
        var store = new TestStore();
        var clock = new FakeClock(now ?? DefaultNow);
        var eventsRepo = new InMemoryEventRepository(store);
        var reservationsRepo = new InMemoryReservationRepository(store);
        var venuesRepo = new InMemoryVenueRepository(store);
        return new TestContext(clock, eventsRepo, reservationsRepo, venuesRepo);
    }
}

internal sealed class FakeClock : IClock
{
    public FakeClock(DateTimeOffset utcNow) => UtcNow = utcNow;
    public DateTimeOffset UtcNow { get; set; }
}

internal sealed class TestStore
{
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

internal sealed class InMemoryEventRepository : IEventRepository
{
    private readonly TestStore _store;
    public InMemoryEventRepository(TestStore store) => _store = store;
    public Task<Event?> GetByIdAsync(int id, CancellationToken cancellationToken = default) => Task.FromResult(_store.Events.FirstOrDefault(e => e.Id == id));
    public Task<IReadOnlyList<Event>> ListAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Event>>(_store.Events.ToArray());
    public Task<Event> AddAsync(Event eventItem, CancellationToken cancellationToken = default)
    {
        eventItem.Id = _store.NextEventId++;
        _store.Events.Add(eventItem);
        return Task.FromResult(eventItem);
    }

    public Task UpdateAsync(Event eventItem, CancellationToken cancellationToken = default) => Task.CompletedTask;
}

internal sealed class InMemoryReservationRepository : IReservationRepository
{
    private readonly TestStore _store;
    public InMemoryReservationRepository(TestStore store) => _store = store;
    public Task<Reservation?> GetByIdAsync(int id, CancellationToken cancellationToken = default) => Task.FromResult(_store.Reservations.FirstOrDefault(r => r.Id == id));
    public Task<IReadOnlyList<Reservation>> ListByEventAsync(int eventId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Reservation>>(_store.Reservations.Where(r => r.EventId == eventId).ToArray());
    public Task<IReadOnlyList<Reservation>> ListAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Reservation>>(_store.Reservations.ToArray());
    public Task<Reservation> AddAsync(Reservation reservation, CancellationToken cancellationToken = default)
    {
        reservation.Id = _store.NextReservationId++;
        _store.Reservations.Add(reservation);
        return Task.FromResult(reservation);
    }

    public Task UpdateAsync(Reservation reservation, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task<bool> ReservationCodeExistsAsync(string code, CancellationToken cancellationToken = default) => Task.FromResult(_store.Reservations.Any(r => r.ReservationCode == code));
}

internal sealed class InMemoryVenueRepository : IVenueRepository
{
    private readonly TestStore _store;
    public InMemoryVenueRepository(TestStore store) => _store = store;
    public Task<Venue?> GetByIdAsync(int id, CancellationToken cancellationToken = default) => Task.FromResult(_store.Venues.FirstOrDefault(v => v.Id == id));
    public Task<IReadOnlyList<Venue>> ListAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Venue>>(_store.Venues.ToArray());
}
