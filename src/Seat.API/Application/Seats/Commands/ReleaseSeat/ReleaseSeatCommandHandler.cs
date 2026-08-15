using System.Text.Json;
using MediatR;
using Seat.API.Domain.Entities;
using Seat.API.Domain.Interfaces;
using StackExchange.Redis;

namespace Seat.API.Application.Seats.Command.ReleaseSeat;

public class ReleaseSeatCommandHandler : IRequestHandler<ReleaseSeatCommand, bool>
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IRedisLockService _lockService;
    private readonly ISeatRepository _seatRepo;
    private readonly ILogger<ReleaseSeatCommandHandler> _logger;

    public ReleaseSeatCommandHandler(
        IConnectionMultiplexer redis,
        ILogger<ReleaseSeatCommandHandler> logger,
        IRedisLockService lockService,
        ISeatRepository seatRepo)
    {
        _redis = redis;
        _logger = logger;
        _lockService = lockService;
        _seatRepo = seatRepo;
    }

    public async Task<bool> Handle(ReleaseSeatCommand request, CancellationToken cancellationToken)
    {
        var mutexToken = await _lockService.AcquireLockAsync(request.showtimeId, request.seatId, TimeSpan.FromSeconds(5));

        if (string.IsNullOrEmpty(mutexToken)) return false;

        try
        {
            var seatLock = await _lockService.GetLockSeatAsync(request.showtimeId, request.seatId);

            if (seatLock is null)
            {
                _logger.LogInformation("Seat not found {showtimeId}", request.showtimeId);
                return false;
            }

            if (seatLock.LockedByUserId != request.userId) return false;
            if (seatLock.LockToken != request.lockToken) return false;

            var seat = await _seatRepo.GetSeatHashAsync(request.showtimeId, request.seatId);
            if (seat is null) return false;

            seat.SeatStatus = SeatStatus.Available;
            seat.LockedByUserId = string.Empty;
            seat.LockToken = string.Empty;
            seat.LockExpiration = default;

            var result = await _lockService.ReleaseLockAsync(request.showtimeId, request.seatId, seatLock.LockToken);

            if (result)
            {
                await _seatRepo.SetSeatHashAysnc(request.showtimeId, request.seatId, JsonSerializer.Serialize(seat));

                // remove 1 seat
                await _seatRepo.RemoveSeatFromReservationHashAsync(request.showtimeId, request.userId, request.seatId);
                _logger.LogInformation("Release seat {seatId} success for showtime {showtimeId}", request.seatId, request.showtimeId);
            }

            return result;
        }
        finally
        {
            await _lockService.ReleaseMutexAsync(request.showtimeId, request.seatId, mutexToken);
        }
    }
}
