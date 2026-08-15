
using BookingService.Domain.AggregateModel.BookingAggregates;
using MediatR;

namespace BookingService.Domain.Events;

public class BookingPaidDomainEvent : INotification
{
    public Booking Booking { get; }

    public BookingPaidDomainEvent(Booking booking)
    {
        Booking = booking ?? throw new ArgumentNullException(nameof(booking));
    }
}
