namespace Seat.API.Application.Seats.Commands.MarkSeatSold;

public class MarkSeatSoldCommandHandler : IRequestHandler<MarkSeatSoldCommand, bool>
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IRedisLockService _lockService;
    private readonly ISeatRepository _seatRepo;
    private readonly ILogger<MarkSeatSoldCommandHandler> _logger;
    private readonly IEventBus _eventBus;

    public MarkSeatSoldCommandHandler(
        IConnectionMultiplexer redis,
        ILogger<MarkSeatSoldCommandHandler> logger,
        IRedisLockService lockService,
        ISeatRepository seatRepo,
        IEventBus eventBus)
    {
        _redis = redis;
        _logger = logger;
        _lockService = lockService;
        _seatRepo = seatRepo;
        _eventBus = eventBus;
    }

    public async Task<bool> Handle(MarkSeatSoldCommand request, CancellationToken cancellationToken)
    {
        var mutexToken = await _lockService.AcquireLockAsync(request.showtimeId, request.seatId, TimeSpan.FromSeconds(5));

        if (string.IsNullOrEmpty(mutexToken))
            return false;

        try
        {
            var seatLockData = await _lockService.GetLockSeatAsync(request.showtimeId, request.seatId);

            if (seatLockData is null)
            {
                var bookingCancel = new BookingCanceledIntegrationEvent(request.showtimeId, request.userId);
                await _eventBus.PublishAsync(bookingCancel);
                return false;
            }

            if (seatLockData.LockedByUserId != request.userId) return false;
            if (seatLockData.LockToken != request.lockToken) return false;

            var seat = await _seatRepo.GetSeatHashAsync(request.showtimeId, request.seatId);

            if (seat is null) return false;

            seat.SeatStatus = SeatStatus.Sold;
            seat.LockedByUserId = string.Empty;
            seat.LockToken = string.Empty;
            seat.LockExpiration = default;

            await _seatRepo.SetSeatHashAysnc(request.showtimeId, request.seatId, JsonSerializer.Serialize(seat));

            return await _lockService.ReleaseLockAsync(request.showtimeId, request.seatId, seatLockData.LockToken);
        }
        finally
        {
            await _lockService.ReleaseMutexAsync(request.showtimeId, request.seatId, mutexToken);
        }
    }
}
