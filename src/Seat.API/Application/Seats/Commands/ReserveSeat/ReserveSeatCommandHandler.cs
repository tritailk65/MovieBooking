
namespace Seat.API.Application.Seats.Command.ReserveSeat;

public class ReserveSeatCommandHandler : IRequestHandler<ReserveSeatApplicationCommand, ReserveSeatsResult>
{
    private readonly IRedisLockService _lockService;
    private readonly ISeatRepository _seatRepo;
    private readonly ILogger<ReserveSeatCommandHandler> _logger;

    public ReserveSeatCommandHandler(
        ILogger<ReserveSeatCommandHandler> logger,
        IRedisLockService lockService,
        ISeatRepository seatRepo)
    {
        _logger = logger;
        _lockService = lockService;
        _seatRepo = seatRepo;
    }

    public async Task<ReserveSeatsResult> Handle (ReserveSeatApplicationCommand request, CancellationToken cancellationToken)
    {
        //Get seat reservation
        var seatReservation = await _seatRepo.GetSeatReservationHashAsync(request.ShowtimeId, request.UserId);

        if (seatReservation is null)
        {
            _logger.LogInformation("Seat reservation not found for showtime {showtimeId} and user {userId}",
                request.ShowtimeId,
                request.UserId);
            return new ReserveSeatsResult(false, "Seat reservation not found");
        }

        if (!IsRequestedReservation(seatReservation, request.ReservationId.ToString()))
        {
            _logger.LogInformation("Seat reservation mismatch for showtime {showtimeId}, user {userId}, reservation {reservationId}",
                request.ShowtimeId,
                request.UserId,
                request.ReservationId);
            return new ReserveSeatsResult(false, "Seat reservation mismatch for showtime");

        }

        //Get all seat of reservation
        var seatIds = seatReservation.SeatIds
            .Where(seatId => !string.IsNullOrWhiteSpace(seatId))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (seatIds.Length == 0)
        {
            _logger.LogInformation("Seat reservation {reservationId} has no seats", seatReservation.Id);
            return new ReserveSeatsResult(false, $"Seat reservation {seatReservation.Id} has no seats");
        }

        if (seatReservation.ExpiresAt <= DateTime.UtcNow)
        {
            _logger.LogInformation("Seat reservation {reservationId} is expired", seatReservation.Id);
            return new ReserveSeatsResult(false, $"Seat reservation {seatReservation.Id} is expired");
        }

        var mutexTokens = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var seatId in seatIds)
        {   
            // Mutext lock when interactive with lock key
            var mutexToken = await _lockService.AcquireLockAsync(request.ShowtimeId, seatId, TimeSpan.FromSeconds(5));

            if (string.IsNullOrEmpty(mutexToken))
            {
                // TODO Test-case: Trường hợp này thêm vào hướng xử lý khi không lấy được mutex token

                _logger.LogInformation("Could not acquire mutex to release seat {seatId} for showtime {showtimeId}",
                    seatId,
                    request.ShowtimeId);
                await ReleaseMutexesAsync(request.ShowtimeId, mutexTokens);
                return new ReserveSeatsResult(false, $"Could not acquire mutex to release seat {seatId} for showtime {request.ShowtimeId}");
            }

            mutexTokens[seatId] = mutexToken;
        }

        try
        {
            foreach (var seatId in seatIds)
            {
                var canConfirm = await CanReserveSeatAsync(request.ShowtimeId, request.UserId, seatId);
                if (!canConfirm) return null;
            }

            foreach (var seatId in seatIds)
            {
                var seatConfirmed = await ReserveSeatAsync(request.ShowtimeId, request.UserId, seatId);
                if (!seatConfirmed) return null;
            }

            _logger.LogInformation("Reserved seat {reservationId} for showtime {showtimeId} and user {userId}",
                seatReservation.Id,
                request.ShowtimeId,
                request.UserId);

            seatReservation.IncreaseReservationVersion();

            return new ReserveSeatsResult(true, $"Reserved seat {seatReservation.Id} for showtime {request.ShowtimeId} and user {request.UserId}");
        }
        finally
        {
            await ReleaseMutexesAsync(request.ShowtimeId, mutexTokens);
        }
    }

    private async Task<bool> CanReserveSeatAsync(int showtimeId, string userId, string seatId)
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

    private async Task<bool> ReserveSeatAsync(int showtimeId, string userId, string seatId)
    {
        var seatLock = await _lockService.GetLockSeatAsync(showtimeId, seatId);
        if (seatLock is null || seatLock.LockedByUserId != userId) return false;

        //Nhả key lock
        //var lockReleased = await _lockService.ReleaseLockAsync(showtimeId, seatId, seatLock.LockToken);
        // TODO Nice-to-have: Trả lỗi rõ ràng cho client
        //if (!lockReleased) return false;

        var seat = await _seatRepo.GetSeatHashAsync(showtimeId, seatId);
        if (seat is null) return false;

        seat.SeatStatus = SeatStatus.Reserved; 
        // Thêm logic reserve seat ở đây

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
