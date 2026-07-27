namespace BookingService.API.Application.Models;

public record SeatReservation
{
    public Guid Id { get; set; }
    public int ShowtimeId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public IEnumerable<string> SeatIds { get; set; } = [];
    public DateTime ExpiresAt { get; set; }
    public int RemainingSeconds {get; set;} //Cho UI count down, gần hết thì báo user gia hạn thêm thời gian cho giao dịch
    public decimal BasePrice {get; set;}

}