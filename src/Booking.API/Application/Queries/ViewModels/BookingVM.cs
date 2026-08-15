namespace BookingService.API.Application.Queries.ViewModels;


public record BookingVM
{
    public int Id { get; set; }
    public string UserId { get; set; }
    public int ShowtimeId { get; set; }
    public int HallId { get; set; }
    public DateTime BookingAt { get; set; }
    public string BookingStatus { get; set; }
    public IEnumerable<BookingItemVM> BookingItems {get; set;}
}
