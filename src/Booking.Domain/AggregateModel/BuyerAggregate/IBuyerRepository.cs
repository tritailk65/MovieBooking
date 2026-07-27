using BookingService.Domain.SeedWork;

namespace BookingService.Domain.AggregateModel.BuyerAggregate;


public interface IBuyerRepository : IRepository<Buyer>
{
    Buyer Add (Buyer buyer);
    Buyer Update (Buyer buyer);
    Task<Buyer> FindAsync (string BuyerIndentityGuid);
    Task<Buyer> FindByIdAsync (int id);
}