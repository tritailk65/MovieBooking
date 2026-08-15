using System.Text.Json;
using MediatR;
using Seat.API.Application.Command.LockSeat;
using Seat.API.Domain.Entities;
using Seat.API.Domain.Interfaces;
using StackExchange.Redis;

namespace Seat.API.Application.Seats.Commands.LockSeat;


// TODO: HIện tại chưa có hàm dọn lock key bị mất Key lock

/// <summary>
/// Command KHoá 1 ghế
/// </summary>

public class LockSeatCommandHandler : IRequestHandler<LockSeatCommand, LockSeatResult>
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IRedisLockService _lockService;
    private readonly ISeatRepository _seatRepo;
    private readonly ILogger<LockSeatCommandHandler> _logger;

    public LockSeatCommandHandler(
        IConnectionMultiplexer redis,
        ILogger<LockSeatCommandHandler> logger,
        IRedisLockService lockService,
        ISeatRepository seatRepo)
    {
        _redis = redis;
        _logger = logger;
        _lockService = lockService;
        _seatRepo = seatRepo;
    }

    public async Task<LockSeatResult> Handle(LockSeatCommand request, CancellationToken cancellationToken)
    {
        var mutexToken = await _lockService.AcquireLockAsync(request.showtimeId, request.seatId, TimeSpan.FromSeconds(5));

        if (string.IsNullOrEmpty(mutexToken))
        {
            _logger.LogInformation("Lock is not available, another process is interacting with this seat");
            return new LockSeatResult(false, null, default);
        }

        try
        {
            //Check key if it exist
            var existingLock = await _lockService.GetLockSeatAsync(request.showtimeId, request.seatId);

            if (existingLock is not null)
                return new LockSeatResult(false, null, default);

            var targetSeat = await _seatRepo.GetSeatHashAsync(request.showtimeId, request.seatId);

            if (targetSeat is null || targetSeat.SeatStatus != SeatStatus.Available)
                return new LockSeatResult(false, null, default);

            var lockToken = Guid.NewGuid().ToString();
            var lockDuration = TimeSpan.FromMinutes(10);
            var lockExpiration = DateTime.UtcNow.Add(lockDuration);

            targetSeat.SeatStatus = SeatStatus.Locked;
            targetSeat.LockedByUserId = request.userId;
            targetSeat.LockToken = lockToken;
            targetSeat.LockExpiration = lockExpiration;

            // Them lock seat
            var result = await _lockService.SetLockSeatAsync(request.showtimeId, request.seatId, JsonSerializer.Serialize(targetSeat), lockDuration);

            if (result)
            {        
                // Tạo reservation trước đẻ tránh trường hợp map lệch với reservation khi add ghế vào reservation xảy ra lỗi
                var seatReservation = await BuildSeatReservationAsync(request, lockExpiration);
                seatReservation.BasePrice = targetSeat.BasePrice;
                await _seatRepo.SetSeatReservationHashAsync(seatReservation);

                // Cập nhật lại hash seat map              
                await _seatRepo.SetSeatHashAysnc(request.showtimeId, request.seatId, JsonSerializer.Serialize(targetSeat));

                return new LockSeatResult(true, targetSeat.LockToken, targetSeat.LockExpiration);
            }

            return new LockSeatResult(false, null, default);
        }
        finally
        {
            await _lockService.ReleaseMutexAsync(request.showtimeId, request.seatId, mutexToken);
        }
    }

    private async Task<SeatReservation> BuildSeatReservationAsync(LockSeatCommand request, DateTime lockExpiration)
    {
        var existingReservation = await _seatRepo.GetSeatReservationHashAsync(request.showtimeId, request.userId);
        var seatIds = new HashSet<string>(StringComparer.Ordinal); //Compare mode 

        // Trường hợp đã có reservation rồi, tức là đã có chọn vài ghế trước đó
        if (existingReservation is not null)
        {
            // Duyệt từng ghế được ghi trong reservation
            foreach (var seatId in existingReservation.SeatIds)
            {
                // Kiểm tra với danh sách lock ghế xem có khớp hay khoong
                var lockedSeat = await _lockService.GetLockSeatAsync(request.showtimeId, seatId);

                // TODO Test-case 1: Nếu ghế trong reservation nhưng không có lock key thì sao?

                if (lockedSeat?.LockedByUserId == request.userId)
                    seatIds.Add(seatId);
            }
        }

        // Add thêm ghế hiện tại được request trong command
        seatIds.Add(request.seatId);

        return new SeatReservation(
            existingReservation?.Id ?? Guid.NewGuid(),
            request.showtimeId,
            request.userId,
            seatIds,
            lockExpiration);
    }
}
