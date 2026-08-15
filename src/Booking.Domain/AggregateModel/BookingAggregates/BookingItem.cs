using BookingService.Domain.SeedWork;
using BookingService.Domain.Exceptions;

namespace BookingService.Domain.AggregateModel.BookingAggregates;

public class BookingItem : Entity
{
    public int Showtime {get; private set;}
    public string SeatId {get; private set;}
    public decimal BasePrice {get; private set;}
    protected BookingItem() { }

    internal BookingItem(int showtimeId, string seatId, decimal basePrice)
    {
        if (showtimeId <= 0)
        {
            throw new BookingDomainException("Showtime id is required.");
        }

        if (string.IsNullOrWhiteSpace(seatId))
        {
            throw new BookingDomainException("Seat id is required.");
        }

        if (basePrice <= 0)
        {
            throw new BookingDomainException("Base price must be greater than 0.");
        }

        Showtime = showtimeId;
        SeatId = seatId;
        BasePrice = basePrice;
    }
}
