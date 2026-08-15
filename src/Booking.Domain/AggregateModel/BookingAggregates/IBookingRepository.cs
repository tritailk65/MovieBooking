

using BookingService.Domain.SeedWork;

namespace BookingService.Domain.AggregateModel.BookingAggregates;

public interface IBookingRepository : IRepository<Booking>
{
    Booking Add(Booking booking);
    void Update(Booking booking);
    Task<Booking> GetByIdAsync(int bookingId);
}
