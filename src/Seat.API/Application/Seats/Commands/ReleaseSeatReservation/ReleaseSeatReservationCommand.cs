using MediatR;

namespace Seat.API.Application.Seats.Command.ReleaseSeatReservation;

public record ReleaseSeatReservationCommand
(
    int showtimeId,
    string reservationId,   
    string userId

) : IRequest<bool>;