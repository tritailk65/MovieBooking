using MediatR;
using Seat.API.Domain.Entities;

namespace Seat.API.Application.Command.ComfirmReservation;

public record ConfirmSeatReservationApplicationCommand(
    Guid ReservationId,
    int BookingId,
    int ShowtimeId,
    string UserId,
    int ReservationVersion) : IRequest<ConfirmSeatReservationResult>;


public sealed record ConfirmSeatReservationResult(
    bool Succeeded,
    string Reason = "");
