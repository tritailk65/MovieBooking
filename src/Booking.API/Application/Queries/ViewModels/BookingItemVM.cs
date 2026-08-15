namespace BookingService.API.Application.Queries.ViewModels;


public record BookingItemVM
{
    public string SeatId {get; set;}
    public decimal BasePrice {get; set;}

}