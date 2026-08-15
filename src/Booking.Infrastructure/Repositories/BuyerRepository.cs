namespace BookingService.Infrastructure.Repository;

public class BuyerRepository : IBuyerRepository
{
    private readonly BookingContext _context;

    public IUnitOfWork UnitOfWork => _context;

    public BuyerRepository(BookingContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public Buyer Add(Buyer buyer)
    {
        if (buyer.IsTransient())
        {
            return _context.Buyers.Add(buyer).Entity;
        }

        return buyer;
    }

    public Buyer Update(Buyer buyer)
    {
        return _context.Buyers.Update(buyer).Entity;

    }

    public async Task<Buyer> FindAsync(string BuyerIndentityGuid)
    {
        var buyer = await _context.Buyers.Include(b => b.PaymentMethods).Where(b => b.IdentityGuid == BuyerIndentityGuid).SingleOrDefaultAsync();

        return buyer;
    }

    public async Task<Buyer> FindByIdAsync(int id)
    {
        var buyer = await _context.Buyers.Include(b => b.PaymentMethods).Where(b => b.Id == id).SingleOrDefaultAsync();

        return buyer;
    }
}