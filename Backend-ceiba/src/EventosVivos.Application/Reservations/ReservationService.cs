using System.Net.Mail;
using EventosVivos.Application.Abstractions;
using EventosVivos.Application.Common;
using EventosVivos.Domain;

namespace EventosVivos.Application.Reservations;

public sealed class ReservationService
{
    private readonly IClock _clock;
    private readonly IEventRepository _events;
    private readonly IReservationRepository _reservations;

    public ReservationService(IClock clock, IEventRepository events, IReservationRepository reservations)
    {
        _clock = clock;
        _events = events;
        _reservations = reservations;
    }

    public async Task<Result<ReservationResponse>> CreateAsync(CreateReservationRequest request, CancellationToken cancellationToken = default)
    {
        var eventItem = await _events.GetByIdAsync(request.EventId, cancellationToken);
        if (eventItem is null)
        {
            return Result<ReservationResponse>.Failure("El evento no existe.", 404);
        }

        eventItem.RefreshStatus(_clock.UtcNow);
        await _events.UpdateAsync(eventItem, cancellationToken);

        var validation = await ValidateReservationAsync(request, eventItem, cancellationToken);
        if (validation is not null)
        {
            return Result<ReservationResponse>.Failure(validation);
        }

        var reservation = new Reservation
        {
            EventId = request.EventId,
            Quantity = request.Quantity,
            BuyerName = request.BuyerName.Trim(),
            BuyerEmail = request.BuyerEmail.Trim(),
            Status = ReservationStatus.PendientePago,
            CreatedAt = _clock.UtcNow
        };

        var created = await _reservations.AddAsync(reservation, cancellationToken);
        return Result<ReservationResponse>.Success(created.ToResponse(), 201);
    }

    public async Task<Result<ReservationResponse>> ConfirmPaymentAsync(int id, CancellationToken cancellationToken = default)
    {
        var reservation = await _reservations.GetByIdAsync(id, cancellationToken);
        if (reservation is null)
        {
            return Result<ReservationResponse>.Failure("La reserva no existe.", 404);
        }

        if (reservation.Status == ReservationStatus.Confirmada)
        {
            return Result<ReservationResponse>.Failure("La reserva ya esta confirmada.");
        }

        if (reservation.Status == ReservationStatus.Cancelada)
        {
            return Result<ReservationResponse>.Failure("La reserva cancelada no puede confirmarse.");
        }

        reservation.Status = ReservationStatus.Confirmada;
        reservation.ConfirmedAt = _clock.UtcNow;
        reservation.ReservationCode = await GenerateUniqueCodeAsync(cancellationToken);
        await _reservations.UpdateAsync(reservation, cancellationToken);

        return Result<ReservationResponse>.Success(reservation.ToResponse());
    }

    public async Task<Result<ReservationResponse>> CancelAsync(int id, CancellationToken cancellationToken = default)
    {
        var reservation = await _reservations.GetByIdAsync(id, cancellationToken);
        if (reservation is null)
        {
            return Result<ReservationResponse>.Failure("La reserva no existe.", 404);
        }

        if (reservation.Status == ReservationStatus.Cancelada)
        {
            return Result<ReservationResponse>.Failure("La reserva ya esta cancelada.");
        }

        if (reservation.Status == ReservationStatus.PendientePago)
        {
            return Result<ReservationResponse>.Failure("Solo se pueden cancelar reservas confirmadas.");
        }

        var eventItem = await _events.GetByIdAsync(reservation.EventId, cancellationToken);
        if (eventItem is null)
        {
            return Result<ReservationResponse>.Failure("El evento asociado no existe.", 404);
        }

        reservation.Status = ReservationStatus.Cancelada;
        reservation.CancelledAt = _clock.UtcNow;
        reservation.LostTickets = eventItem.StartAt - _clock.UtcNow < TimeSpan.FromHours(48)
            ? reservation.Quantity
            : 0;

        await _reservations.UpdateAsync(reservation, cancellationToken);
        return Result<ReservationResponse>.Success(reservation.ToResponse());
    }

    public async Task<Result<OccupancyReportResponse>> GetOccupancyReportAsync(int eventId, CancellationToken cancellationToken = default)
    {
        var eventItem = await _events.GetByIdAsync(eventId, cancellationToken);
        if (eventItem is null)
        {
            return Result<OccupancyReportResponse>.Failure("El evento no existe.", 404);
        }

        eventItem.RefreshStatus(_clock.UtcNow);
        await _events.UpdateAsync(eventItem, cancellationToken);

        var reservations = await _reservations.ListByEventAsync(eventId, cancellationToken);
        var soldTickets = reservations
            .Where(r => r.Status == ReservationStatus.Confirmada)
            .Sum(r => r.Quantity);
        var lostTickets = reservations.Sum(r => r.LostTickets);
        var availableTickets = eventItem.MaxCapacity - soldTickets - lostTickets;
        var occupancyPercentage = eventItem.MaxCapacity == 0
            ? 0
            : Math.Round((decimal)soldTickets / eventItem.MaxCapacity * 100, 2);
        var income = soldTickets * eventItem.Price;

        return Result<OccupancyReportResponse>.Success(new OccupancyReportResponse(
            eventItem.Id,
            eventItem.Title,
            soldTickets,
            lostTickets,
            availableTickets,
            occupancyPercentage,
            income,
            eventItem.Status.ToApiValue()));
    }

    private async Task<string?> ValidateReservationAsync(CreateReservationRequest request, Event eventItem, CancellationToken cancellationToken)
    {
        if (eventItem.Status != EventStatus.Activo)
        {
            return "Solo se pueden reservar eventos activos.";
        }

        if (request.Quantity <= 0)
        {
            return "La cantidad debe ser 1 o mas.";
        }

        if (string.IsNullOrWhiteSpace(request.BuyerName))
        {
            return "El nombre del comprador es obligatorio.";
        }

        if (!IsValidEmail(request.BuyerEmail))
        {
            return "El email del comprador no tiene un formato valido.";
        }

        var timeToStart = eventItem.StartAt - _clock.UtcNow;
        if (timeToStart < TimeSpan.FromHours(1))
        {
            return "No se permiten reservas para eventos que inicien en menos de 1 hora.";
        }

        if (timeToStart < TimeSpan.FromHours(24) && request.Quantity > 5)
        {
            return "Para eventos que inician en menos de 24 horas solo se permiten maximo 5 entradas por transaccion.";
        }

        if (timeToStart >= TimeSpan.FromHours(24) && eventItem.Price > 100 && request.Quantity > 10)
        {
            return "Los eventos con precio mayor a $100 limitan a maximo 10 entradas por transaccion.";
        }

        var reservations = await _reservations.ListByEventAsync(eventItem.Id, cancellationToken);
        var usedTickets = reservations
            .Where(r => r.Status == ReservationStatus.Confirmada || r.Status == ReservationStatus.PendientePago)
            .Sum(r => r.Quantity);
        var lostTickets = reservations.Sum(r => r.LostTickets);
        var availableTickets = eventItem.MaxCapacity - usedTickets - lostTickets;

        return request.Quantity > availableTickets
            ? "No hay entradas disponibles suficientes."
            : null;
    }

    private async Task<string> GenerateUniqueCodeAsync(CancellationToken cancellationToken)
    {
        string code;
        do
        {
            code = $"EV-{Random.Shared.Next(0, 1_000_000):D6}";
        }
        while (await _reservations.ReservationCodeExistsAsync(code, cancellationToken));

        return code;
    }

    private static bool IsValidEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        try
        {
            var address = new MailAddress(email);
            return address.Address == email.Trim();
        }
        catch
        {
            return false;
        }
    }
}
