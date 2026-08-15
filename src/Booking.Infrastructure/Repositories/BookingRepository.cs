namespace BookingService.Infrastructure.Repository;

public class BookingRepository : IBookingRepository
{
    private readonly BookingContext _context;

    public IUnitOfWork UnitOfWork => _context;

    public BookingRepository(BookingContext context)
    {
        _context = context;
    }

    public Booking Add(Booking booking)
    {
        return _context.Bookings.Add(booking).Entity;
    }

    public void Update(Booking booking)
    {
        _context.Entry(booking).State = EntityState.Modified;
    }

    public async Task<Booking> GetByIdAsync(int bookingId)
    {
        var booking = await _context.Bookings.FindAsync(bookingId);
    
        if (booking != null)
        {
            await _context.Entry(booking).Collection(i => i.BookingItems).LoadAsync();
            await _context.Entry(booking).Reference(i => i.BookingStatus).LoadAsync();
        }

        return booking;
    }
}
