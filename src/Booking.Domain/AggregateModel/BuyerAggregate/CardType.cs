
using BookingService.Domain.SeedWork;

namespace BookingService.Domain.AggregateModel.BuyerAggregate;

public sealed class CardType : Enumeration
{
    public static readonly CardType Visa = new(2, nameof(Visa));
    public static readonly CardType MasterCard = new(3, nameof(MasterCard));
    public CardType(int id, string name) : base(id, name)
    {
    }
}
