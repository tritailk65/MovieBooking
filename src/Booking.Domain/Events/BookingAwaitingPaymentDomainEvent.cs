using BookingService.Domain.AggregateModel.BookingAggregates;
using MediatR;

namespace BookingService.Domain.Events;

public class BookingAwaitingPaymentDomainEvent : INotification
{
    public Booking Booking { get; }

    public BookingAwaitingPaymentDomainEvent(Booking booking)
    {
        Booking = booking ?? throw new ArgumentNullException(nameof(booking));
    }
}
