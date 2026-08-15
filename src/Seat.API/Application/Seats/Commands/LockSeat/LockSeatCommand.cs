using MediatR;
using Seat.API.Domain.Entities;

namespace Seat.API.Application.Command.LockSeat;

public record LockSeatCommand(int showtimeId, string seatId, string userId) : IRequest<LockSeatResult>;
