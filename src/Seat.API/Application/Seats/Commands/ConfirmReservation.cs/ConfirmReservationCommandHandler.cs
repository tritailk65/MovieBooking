namespace Seat.API.Application.Command.ComfirmReservation;

public class ConfirmReservationCommandHandler : IRequestHandler<ConfirmReservationCommand, SeatReservation>
{
    private readonly IRedisLockService _lockService;
    private readonly ISeatRepository _seatRepo;
    private readonly ILogger<ConfirmReservationCommandHandler> _logger;

    public ConfirmReservationCommandHandler(
        ILogger<ConfirmReservationCommandHandler> logger,
        IRedisLockService lockService,
        ISeatRepository seatRepo)
    {
        _logger = logger;
        _lockService = lockService;
        _seatRepo = seatRepo;
    }

    public async Task<SeatReservation> Handle (ConfirmReservationCommand request, CancellationToken cancellationToken)
    {
        //Get seat reservation
        var seatReservation = await _seatRepo.GetSeatReservationHashAsync(request.showtimeId, request.userId);

        if (seatReservation is null)
        {
            _logger.LogInformation("Seat reservation not found for showtime {showtimeId} and user {userId}",
                request.showtimeId,
                request.userId);
            return null;
        }

        if (!IsRequestedReservation(seatReservation, request.reservationId))
        {
            _logger.LogInformation("Seat reservation mismatch for showtime {showtimeId}, user {userId}, reservation {reservationId}",
                request.showtimeId,
                request.userId,
                request.reservationId);
            return null;
        }

        //Get all seat of reservation
        var seatIds = seatReservation.SeatIds
            .Where(seatId => !string.IsNullOrWhiteSpace(seatId))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (seatIds.Length == 0)
        {
            _logger.LogInformation("Seat reservation {reservationId} has no seats", seatReservation.Id);
            return null;
        }

        if (seatReservation.ExpiresAt <= DateTime.UtcNow)
        {
            _logger.LogInformation("Seat reservation {reservationId} is expired", seatReservation.Id);
            return null;
        }

        var mutexTokens = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var seatId in seatIds)
        {   
            // Mutext lock when interactive with lock key
            var mutexToken = await _lockService.AcquireLockAsync(request.showtimeId, seatId, TimeSpan.FromSeconds(5));

            if (string.IsNullOrEmpty(mutexToken))
            {
                // TODO Test-case: Trường hợp này thêm vào hướng xử lý khi không lấy được mutex token

                _logger.LogInformation("Could not acquire mutex to release seat {seatId} for showtime {showtimeId}",
                    seatId,
                    request.showtimeId);
                await ReleaseMutexesAsync(request.showtimeId, mutexTokens);
                return null;
            }

            mutexTokens[seatId] = mutexToken;
        }

        try
        {
            foreach (var seatId in seatIds)
            {
                var canConfirm = await CanConfirmSeatAsync(request.showtimeId, request.userId, seatId);
                if (!canConfirm) return null;
            }

            foreach (var seatId in seatIds)
            {
                var seatConfirmed = await ConfirmSeatAsync(request.showtimeId, request.userId, seatId);
                if (!seatConfirmed) return null;
            }

            var reservationReleased = await _seatRepo.ReleaseSeatReservationHashAsync(request.showtimeId, request.userId);

            if (!reservationReleased) return null;

            _logger.LogInformation("Confirmed seat reservation {reservationId} for showtime {showtimeId} and user {userId}",
                seatReservation.Id,
                request.showtimeId,
                request.userId);

            return seatReservation;
        }
        finally
        {
            await ReleaseMutexesAsync(request.showtimeId, mutexTokens);
        }
    }

    private async Task<bool> CanConfirmSeatAsync(int showtimeId, string userId, string seatId)
    {
        var seatLock = await _lockService.GetLockSeatAsync(showtimeId, seatId);
        var seat = await _seatRepo.GetSeatHashAsync(showtimeId, seatId);

        if (seat is null)
        {
            _logger.LogInformation("Seat {seatId} not found for showtime {showtimeId}", seatId, showtimeId);
            return false;
        }

        if (seatLock is null)
        {
            _logger.LogInformation("Lock key not found for seat {seatId} and showtime {showtimeId}",
                seatId,
                showtimeId);
            return false;
        }

        if (seatLock.LockedByUserId != userId)
        {
            _logger.LogInformation("Seat {seatId} for showtime {showtimeId} is locked by another user",
                seatId,
                showtimeId);
            return false;
        }

        if (seat.SeatStatus == SeatStatus.Sold)
        {
            _logger.LogInformation("Seat {seatId} for showtime {showtimeId} is already sold",
                seatId,
                showtimeId);
            return false;
        }

        return true;
    }

    private async Task<bool> ConfirmSeatAsync(int showtimeId, string userId, string seatId)
    {
        var seatLock = await _lockService.GetLockSeatAsync(showtimeId, seatId);
        if (seatLock is null || seatLock.LockedByUserId != userId) return false;

        //Nhả key lock
        var lockReleased = await _lockService.ReleaseLockAsync(showtimeId, seatId, seatLock.LockToken);
        // TODO Nice-to-have: Trả lỗi rõ ràng cho client
        if (!lockReleased) return false;

        var seat = await _seatRepo.GetSeatHashAsync(showtimeId, seatId);
        if (seat is null) return false;

        seat.SeatStatus = SeatStatus.Sold;  // Chuyển status sang đã bán
        seat.LockedByUserId = string.Empty;
        seat.LockToken = string.Empty;
        seat.LockExpiration = default;

        // Cập nhật lại map

        // TODO: Chỉnh lại trả về list các ghế cần update sau đó gọi seat service update đúng 1 lần để đảm bảo Stale cho UI
        await _seatRepo.SetSeatHashAysnc(showtimeId, seatId, JsonSerializer.Serialize(seat));

        _logger.LogInformation("Confirmed seat {seatId} for showtime {showtimeId}", seatId, showtimeId);
        return true;
    }

    private async Task ReleaseMutexesAsync(int showtimeId, IReadOnlyDictionary<string, string> mutexTokens)
    {
        foreach (var (seatId, mutexToken) in mutexTokens)
        {
            await _lockService.ReleaseMutexAsync(showtimeId, seatId, mutexToken);
        }
    }

    private static bool IsRequestedReservation(SeatReservation seatReservation, string reservationId)
    {
        return string.IsNullOrWhiteSpace(reservationId)
            || string.Equals(seatReservation.Id.ToString(), reservationId, StringComparison.OrdinalIgnoreCase);
    }
}
