using MediatR;

namespace Seat.API.Application.Seats.Commands.MarkSeatSold;

public record MarkSeatSoldCommand (
    int showtimeId, 
    string seatId,
    string userId, 
    string lockToken) : IRequest<bool>;