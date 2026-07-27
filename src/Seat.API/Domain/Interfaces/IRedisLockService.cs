namespace Seat.API.Domain.Interfaces;

public interface IRedisLockService
{
    Task<string> AcquireLockAsync(int showtimeId, string seatId, TimeSpan expiration);
    Task<bool> SetLockSeatAsync(int showtimeId, string seatId, string value, TimeSpan expiration);
    Task<Entities.Seat> GetLockSeatAsync(int showtimeId, string seatId);
    Task<bool> ReleaseMutexAsync(int showtimeId, string seatId, string mutexToken);
    Task<bool> ReleaseLockAsync(int showtimeId, string seatId, string lockToken);
}
