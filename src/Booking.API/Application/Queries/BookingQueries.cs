using BookingService.API.Application.Queries.ViewModels;

namespace BookingService.API.Application.Queries;

public class BookingQueries : IBookingQueries
{
    private readonly BookingContext _context;

    public BookingQueries(BookingContext context)
    {
        _context = context;
    }

    public Task<BookingVM> GetBookingAsync(int id)
    {
        var booking =  _context.Bookings.Include(m => m.BookingItems).Where(x => x.Id.Equals(id))
            .Select(b => new BookingVM
            {
                Id = b.Id,
                UserId = b.UserId,
                ShowtimeId = b.ShowtimeId,
                HallId = b.HallId,
                BookingAt = b.BookingAt,
                BookingStatus = b.BookingStatus.Name,
                BookingItems = b.BookingItems.Select(bi => new BookingItemVM
                {
                    SeatId = bi.SeatId,
                    BasePrice = bi.BasePrice
                })
            }).FirstOrDefaultAsync();

        if(booking is null)
        {
            throw new KeyNotFoundException();
        }

        return booking;
    }

    public Task<int?> GetBookingIdByReservationAsync(Guid reservationId)
    {
        return _context.Bookings
            .AsNoTracking()
            .Where(booking => booking.ReservationId == reservationId)
            .Select(booking => (int?)booking.Id)
            .SingleOrDefaultAsync();
    }

    public async Task<IEnumerable<BookingVM>> GetBookingFromUserAsync(string userId)
    {
        var booking = _context.Bookings.Include(m => m.BookingItems).Where(b => b.UserId == userId)
            .Select(b => new BookingVM
            {
                Id = b.Id,
                UserId = b.UserId,
                ShowtimeId = b.ShowtimeId,
                HallId = b.HallId,
                BookingAt = b.BookingAt,
                BookingStatus = b.BookingStatus.Name,
                BookingItems = b.BookingItems.Select(bi => new BookingItemVM
                {
                    SeatId = bi.SeatId,
                    BasePrice = bi.BasePrice
                })
            }).ToList();

        if (booking is null)
        {
            throw new KeyNotFoundException();
        }

        return booking;

    }

    public async Task<IEnumerable<CardTypeVM>> GetCardTypesAsync()
    {
        return await _context.CardTypes.Select(c => new CardTypeVM {Id = c.Id, Name = c.Name}).ToListAsync();
    }
}
