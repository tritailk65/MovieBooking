namespace Seat.API.Domain.Entities;

public class ShowtimeSeat
{
    public int Id {get; set;}
    public int ShowtimeId { get; set; }
    public IEnumerable<Seat> Seats { get; set; }
    // Chưa cần những thông tin này trong header
    public SeatStatus Status { get; set; }
    public string LockedByUserId { get; set; }  //Using for testing
    public DateTime? LockExpiration { get; set; } //Using for UI Count down

    /// <summary>
    /// DDD method to determine if the seat can be locked. A seat can be locked if it is currently available or if it is locked but the lock has expired.
    /// </summary>
    /// <returns></returns>
    public bool CanBeLocked ()
    {
        if (Status == SeatStatus.Available)
            return true;

        if (Status == SeatStatus.Locked && LockExpiration.HasValue && LockExpiration.Value < DateTime.UtcNow)
            return true;
        
        return false;
    }

    public void Lock(string userId, TimeSpan lockDuration)
    {
        if(!CanBeLocked())
            throw new InvalidOperationException("Seat cannot be locked.");

        Status = SeatStatus.Locked;
        LockedByUserId = userId;
        LockExpiration = DateTime.UtcNow.Add(lockDuration);
    }

    public void MarkAsSold()
    {
        Status = SeatStatus.Sold;
        LockedByUserId = null;
        LockExpiration = null;
    }
}
