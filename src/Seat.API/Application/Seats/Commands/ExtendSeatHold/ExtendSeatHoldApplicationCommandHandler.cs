namespace Seat.API.Application.Seats.Command.ExtendSeatHold;

public class ExtendSeatHoldApplicationCommandHandler : IRequestHandler<ExtendSeatHoldApplicationCommand, ExtendSeatHoldResult>
{
    private static readonly TimeSpan MutexDuration = TimeSpan.FromSeconds(5);

    private readonly IRedisLockService _lockService;
    private readonly ISeatRepository _seatRepo;
    private readonly ILogger<ExtendSeatHoldApplicationCommandHandler> _logger;

    public ExtendSeatHoldApplicationCommandHandler(
        ILogger<ExtendSeatHoldApplicationCommandHandler> logger,
        IRedisLockService lockService,
        ISeatRepository seatRepo)
    {
        _logger = logger;
        _lockService = lockService;
        _seatRepo = seatRepo;
    }

    public async Task<ExtendSeatHoldResult> Handle(
        ExtendSeatHoldApplicationCommand request,
        CancellationToken cancellationToken)
    {
        if (request.ExtensionDuration <= TimeSpan.Zero)
        {
            return new ExtendSeatHoldResult(false, "Seat hold extension duration must be greater than zero");
        }

        var seatReservation = await _seatRepo.GetSeatReservationHashAsync(request.ShowtimeId, request.UserId);
        if (seatReservation is null)
        {
            _logger.LogInformation(
                "Seat reservation not found for showtime {ShowtimeId} and user {UserId}",
                request.ShowtimeId,
                request.UserId);
            return new ExtendSeatHoldResult(false, "Seat reservation not found");
        }

        if (seatReservation.Id != request.ReservationId)
        {
            _logger.LogInformation(
                "Seat reservation mismatch for showtime {ShowtimeId}, user {UserId}, reservation {ReservationId}",
                request.ShowtimeId,
                request.UserId,
                request.ReservationId);
            return new ExtendSeatHoldResult(false, "Seat reservation mismatch");
        }

        var now = DateTime.UtcNow;
        if (seatReservation.ExpiresAt <= now)
        {
            _logger.LogInformation("Seat reservation {ReservationId} is expired", seatReservation.Id);
            return new ExtendSeatHoldResult(false, "Seat reservation is expired");
        }

        var seatIds = seatReservation.SeatIds
            .Where(seatId => !string.IsNullOrWhiteSpace(seatId))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(seatId => seatId, StringComparer.Ordinal)
            .ToArray();

        if (seatIds.Length == 0)
        {
            return new ExtendSeatHoldResult(false, "Seat reservation has no seats");
        }

        var mutexTokens = new Dictionary<string, string>(StringComparer.Ordinal);

        try
        {
            foreach (var seatId in seatIds)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var mutexToken = await _lockService.AcquireLockAsync(
                    request.ShowtimeId,
                    seatId,
                    MutexDuration);

                if (string.IsNullOrEmpty(mutexToken))
                {
                    _logger.LogInformation(
                        "Could not acquire mutex to extend seat {SeatId} for showtime {ShowtimeId}",
                        seatId,
                        request.ShowtimeId);
                    return new ExtendSeatHoldResult(false, $"Could not acquire mutex for seat {seatId}");
                }

                mutexTokens[seatId] = mutexToken;
            }

            // Read again after acquiring every mutex so a stale reservation is not extended.
            seatReservation = await _seatRepo.GetSeatReservationHashAsync(request.ShowtimeId, request.UserId);
            now = DateTime.UtcNow;

            if (seatReservation is null ||
                seatReservation.Id != request.ReservationId ||
                seatReservation.ExpiresAt <= now)
            {
                return new ExtendSeatHoldResult(false, "Seat reservation no longer exists or has expired");
            }

            var seats = new List<Domain.Entities.Seat>(seatIds.Length);
            foreach (var seatId in seatIds)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var seat = await _seatRepo.GetSeatHashAsync(request.ShowtimeId, seatId);
                if (seat is null ||
                    seat.SeatStatus == SeatStatus.Sold ||
                    !string.Equals(seat.LockedByUserId, request.UserId, StringComparison.Ordinal))
                {
                    return new ExtendSeatHoldResult(false, $"Seat {seatId} is no longer held by the user");
                }

                seats.Add(seat);
            }

            // Extend from the current deadline. Since the reservation is still active,
            // this guarantees at least ExtensionDuration remains from the current time.
            var newExpiration = seatReservation.ExpiresAt.Add(request.ExtensionDuration);

            foreach (var seat in seats)
            {
                seat.LockExpiration = newExpiration;
                await _seatRepo.SetSeatHashAysnc(
                    request.ShowtimeId,
                    seat.SeatId,
                    JsonSerializer.Serialize(seat));
            }

            seatReservation.ExpiresAt = newExpiration;
            seatReservation.RemainingSeconds = Math.Max(
                1,
                Convert.ToInt32(Math.Ceiling((newExpiration - now).TotalSeconds)));

            // This updates both the reservation value and its Redis key TTL.
            await _seatRepo.SetSeatReservationHashAsync(seatReservation);

            _logger.LogInformation(
                "Extended seat reservation {ReservationId} until {ExpiresAt} by {ExtensionDuration}",
                seatReservation.Id,
                newExpiration,
                request.ExtensionDuration);

            return new ExtendSeatHoldResult(true);
        }
        finally
        {
            foreach (var (seatId, mutexToken) in mutexTokens)
            {
                await _lockService.ReleaseMutexAsync(request.ShowtimeId, seatId, mutexToken);
            }
        }
    }
}
