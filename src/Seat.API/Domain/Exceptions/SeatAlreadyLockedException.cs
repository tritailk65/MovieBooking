namespace Seat.API.Domain.Exceptions;

public class SeatAlreadyLockedException : Exception
{
    public SeatAlreadyLockedException(int showtimeId, int seatId)
        : base($"Seat {seatId} for showtime {showtimeId} is already locked.")
    {
    }
}