using BookingService.Domain.SeedWork;

namespace BookingService.Domain.AggregateModel.BookingAggregates;

public sealed class BookingStatus : Enumeration
{
    public static readonly BookingStatus Submitted = new(1, nameof(Submitted).ToLowerInvariant());
    public static readonly BookingStatus AwaitingSeatValidation = new(2, nameof(AwaitingSeatValidation).ToLowerInvariant());
    public static readonly BookingStatus SeatConfirmed = new(3, nameof(SeatConfirmed).ToLowerInvariant());
    public static readonly BookingStatus Paid = new(4, nameof(Paid).ToLowerInvariant());
    public static readonly BookingStatus Cancelled = new(5, nameof(Cancelled).ToLowerInvariant());

    public BookingStatus(int id, string name) : base(id, name)
    {
    }
}
