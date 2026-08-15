using MediatR;

namespace Seat.API.Application.Seats.Command.ReleaseSeat;

public record ReleaseSeatCommand (
    int showtimeId,
    string seatId ,
    string userId ,
    string lockToken) : IRequest<bool>;
