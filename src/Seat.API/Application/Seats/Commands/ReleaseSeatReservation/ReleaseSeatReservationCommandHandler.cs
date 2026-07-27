using System.Text.Json;
using MediatR;
using Seat.API.Domain.Entities;
using Seat.API.Domain.Interfaces;

namespace Seat.API.Application.Seats.Command.ReleaseSeatReservation;

/// ValidateReservation: trước khi tạo booking/payment
// ConfirmSeatReservation: payment success -> Sold
// ReleaseSeatReservation: payment failed/cancel -> Available
// ExpireReservationCleanup: hết TTL -> Available + socket


/// <summary>
/// Command sử lý nhả ghế khi payment fail, user cancel booking
/// </summary>
/// 

/// TODO: Bắn socket báo ghế trống
public class ReleaseSeatReservationCommandHandler : IRequestHandler<ReleaseSeatReservationCommand, bool>
{
    private readonly IRedisLockService _lockService;
    private readonly ISeatRepository _seatRepo;
    private readonly ILogger<ReleaseSeatReservationCommandHandler> _logger;

    public ReleaseSeatReservationCommandHandler(
        ILogger<ReleaseSeatReservationCommandHandler> logger,
        IRedisLockService lockService,
        ISeatRepository seatRepo)
    {
        _logger = logger;
        _lockService = lockService;
        _seatRepo = seatRepo;
    }

    public async Task<bool> Handle(ReleaseSeatReservationCommand request, CancellationToken cancellationToken)
    {
        //Get seat reservation
        var seatReservation = await _seatRepo.GetSeatReservationHashAsync(request.showtimeId, request.userId);

        if (seatReservation is null)
        {
            _logger.LogInformation("Seat reservation not found for showtime {showtimeId} and user {userId}",
                request.showtimeId,
                request.userId);
            return false;
        }

        if (!IsRequestedReservation(seatReservation, request.reservationId))
        {
            _logger.LogInformation("Seat reservation mismatch for showtime {showtimeId}, user {userId}, reservation {reservationId}",
                request.showtimeId,
                request.userId,
                request.reservationId);
            return false;
        }

        //Get all seat of reservation
        var seatIds = seatReservation.SeatIds
            .Where(seatId => !string.IsNullOrWhiteSpace(seatId))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        var allSeatsReleased = true;

        foreach (var seatId in seatIds)
        {   
            // Mutext lock when interactive with lock key
            var mutexToken = await _lockService.AcquireLockAsync(request.showtimeId, seatId, TimeSpan.FromSeconds(5));

            if (string.IsNullOrEmpty(mutexToken))
            {
                // TODO Test-case: Trường hợp này thêm vào hướng xử lý khi không lấy được mutex token

                allSeatsReleased = false;
                _logger.LogInformation("Could not acquire mutex to release seat {seatId} for showtime {showtimeId}",
                    seatId,
                    request.showtimeId);
                continue;
            }

            try
            {
                var seatReleased = await ReleaseSeatAsync(request.showtimeId, request.userId, seatId);
                allSeatsReleased = allSeatsReleased && seatReleased;
            }
            finally
            {
                await _lockService.ReleaseMutexAsync(request.showtimeId, seatId, mutexToken);
            }
        }

        if (!allSeatsReleased) return false;

        var reservationReleased = await _seatRepo.ReleaseSeatReservationHashAsync(request.showtimeId, request.userId);

        if (reservationReleased)
        {
            _logger.LogInformation("Released seat reservation {reservationId} for showtime {showtimeId} and user {userId}",
                seatReservation.Id,
                request.showtimeId,
                request.userId);
        }

        return reservationReleased && allSeatsReleased;
    }

    private async Task<bool> ReleaseSeatAsync(int showtimeId, string userId, string seatId)
    {
        var seatLock = await _lockService.GetLockSeatAsync(showtimeId, seatId);
        var seat = await _seatRepo.GetSeatHashAsync(showtimeId, seatId);

        if (seat is null)
        {
            _logger.LogInformation("Seat {seatId} not found for showtime {showtimeId}", seatId, showtimeId);
            return false;
        }

        if (seat.SeatStatus == SeatStatus.Sold)
        {
            _logger.LogInformation("Seat {seatId} for showtime {showtimeId} is already sold",
                seatId,
                showtimeId);
            return false;
        }

        if (seatLock is not null)
        {
            if (seatLock.LockedByUserId != userId)
            {
                _logger.LogInformation("Seat {seatId} for showtime {showtimeId} is locked by another user",
                    seatId,
                    showtimeId);
                return false;
            }

            //Nhả key lock
            var lockReleased = await _lockService.ReleaseLockAsync(showtimeId, seatId, seatLock.LockToken);
            // TODO Nice-to-have: Trả lỗi rõ ràng cho client
            if (!lockReleased) return false;
        }
        else if (seat.LockedByUserId != userId)
        {
            return true;
        }

        seat.SeatStatus = SeatStatus.Available;  // Chuyển status sang available để user khác order
        seat.LockedByUserId = string.Empty;
        seat.LockToken = string.Empty;
        seat.LockExpiration = default;

        // Cập nhật lại map

        // TODO: Chỉnh lại trả về list các ghế cần update sau đó gọi seat service update đúng 1 lần để đảm bảo Stale cho UI
        await _seatRepo.SetSeatHashAysnc(showtimeId, seatId, JsonSerializer.Serialize(seat));

        _logger.LogInformation("Released seat {seatId} for showtime {showtimeId}", seatId, showtimeId);
        return true;
    }

    private static bool IsRequestedReservation(SeatReservation seatReservation, string reservationId)
    {
        return string.IsNullOrWhiteSpace(reservationId)
            || string.Equals(seatReservation.Id.ToString(), reservationId, StringComparison.OrdinalIgnoreCase);
    }
}
