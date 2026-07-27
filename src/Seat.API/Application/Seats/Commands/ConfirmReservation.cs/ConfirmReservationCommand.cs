using MediatR;
using Seat.API.Domain.Entities;

namespace Seat.API.Application.Command.ComfirmReservation;

public record ConfirmReservationCommand
(
    int showtimeId,
    string reservationId,   
    string userId
) : IRequest<SeatReservation>;