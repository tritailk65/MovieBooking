namespace Seat.API.Domain.Entities;

public class SeatReservation
{
    public Guid Id { get; set; }
    public int ShowtimeId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public IEnumerable<string> SeatIds { get; set; } = [];
    public DateTime ExpiresAt { get; set; }
    public int RemainingSeconds {get; set;} //Cho UI count down, gần hết thì báo user gia hạn thêm thời gian cho giao dịch
    public decimal BasePrice {get; set;}
    public int ReservationReleased {get; set;}
    public int ReservationVersion {get; init ;} 

    public void IncreaseReservationVersion()
    {
        this.ReservationReleased += 1;
    }

    public SeatReservation()
    {
    }

    public SeatReservation(
        Guid reservationId, 
        int showtimeId, 
        string userId, 
        IEnumerable<string> seatIds, 
        DateTime expiresAt)
    {
        Id = reservationId;
        ShowtimeId = showtimeId;
        UserId = userId;
        SeatIds = seatIds;
        ExpiresAt = expiresAt;
    }
}
