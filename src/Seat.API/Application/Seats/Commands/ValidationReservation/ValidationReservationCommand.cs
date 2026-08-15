using MediatR;
using Seat.API.Domain.Entities;

namespace Seat.API.Application.Seats.Command.ReleaseSeatReservation;

public record ValidationReservationCommand
(
    int showtimeId,
    string reservationId,   
    string userId

) : IRequest<SeatReservation>;