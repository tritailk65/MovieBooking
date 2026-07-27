
using BookingService.Domain.AggregateModel.BookingAggregates;
using MediatR;

namespace BookingService.Domain.Events;

public class BookingSeatConfirmedDomainEvent : INotification
{
    public Booking Booking { get; }

    public BookingSeatConfirmedDomainEvent(Booking booking)
    {
        Booking = booking ?? throw new ArgumentNullException(nameof(booking));
    }
}
