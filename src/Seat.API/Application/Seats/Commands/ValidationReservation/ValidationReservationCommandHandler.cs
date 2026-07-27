using MediatR;
using Seat.API.Application.Seats.Command.ReleaseSeatReservation;
using Seat.API.Domain.Entities;
using Seat.API.Domain.Interfaces;

namespace Seat.API.Application.Command.ValidationReservation;

// TODO: Chưa trả về base price 

/// <summary>
/// Hàm dùng để kiểm tra reservation và trả dữ liệu về cho booking service
/// </summary>
public class ValidationReservationCommandHandler : IRequestHandler<ValidationReservationCommand, SeatReservation>
{
    private readonly IRedisLockService _lockService;
    private readonly ISeatRepository _seatRepo;
    private readonly ILogger<ValidationReservationCommandHandler> _logger;

    public ValidationReservationCommandHandler(
        ILogger<ValidationReservationCommandHandler> logger,
        IRedisLockService lockService,
        ISeatRepository seatRepo)
    {
        _logger = logger;
        _lockService = lockService;
        _seatRepo = seatRepo;
    }

    public async Task<SeatReservation> Handle(ValidationReservationCommand request, CancellationToken cancellationToken)
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

        var seatIds = seatReservation.SeatIds
            .Where(seatId => !string.IsNullOrWhiteSpace(seatId))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (seatIds.Length == 0)
        {
            _logger.LogInformation("Seat reservation {reservationId} has no seats", seatReservation.Id);
            return null;
        }

        var remainingSeconds = Convert.ToInt32(Math.Floor((seatReservation.ExpiresAt - DateTime.UtcNow).TotalSeconds));
        if (remainingSeconds <= 0)
        {
            _logger.LogInformation("Seat reservation {reservationId} is expired", seatReservation.Id);
            return null;
        }

        foreach (var seatId in seatIds)
        {   
            // Check lock key 
            var seatLock = await _lockService.GetLockSeatAsync(request.showtimeId, seatId);
            if (seatLock is null)
            {
                // Kích hoạt lại key ? hoặc trả về null ?
                _logger.LogInformation("Lock key not found for seat {seatId} and showtime {showtimeId}",
                    seatId,
                    request.showtimeId);

                return null;
            }

            if (seatLock.LockedByUserId != request.userId)
            {
                _logger.LogInformation("Seat {seatId} for showtime {showtimeId} is locked by another user",
                    seatId,
                    request.showtimeId);

                return null;
            }
        }

        // thời gian còn lại để thanh toán
        // để hỏi người dùng trong trường hợp cần thêm thời gian để thanh toán 
        seatReservation.RemainingSeconds = remainingSeconds;

        return seatReservation;
    }

    private static bool IsRequestedReservation(SeatReservation seatReservation, string reservationId)
    {
        return string.IsNullOrWhiteSpace(reservationId)
            || string.Equals(seatReservation.Id.ToString(), reservationId, StringComparison.OrdinalIgnoreCase);
    }
}
