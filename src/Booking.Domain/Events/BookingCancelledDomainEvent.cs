using BookingService.Domain.AggregateModel.BookingAggregates;
using MediatR;

namespace BookingService.Domain.Events;

public class BookingCancelledDomainEvent : INotification
{
    public Booking Booking { get; }

    public BookingCancelledDomainEvent(Booking booking)
    {
        Booking = booking ?? throw new ArgumentNullException(nameof(booking));
    }
}
