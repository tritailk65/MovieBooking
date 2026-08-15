
using BookingService.Domain.AggregateModel.BookingAggregates;
using MediatR;

namespace BookingService.Domain.Events;

//Event user begin to booking
public class BookingCreatedDomainEvent : INotification
{
    public Booking Booking { get; }

    public BookingCreatedDomainEvent(Booking booking)
    {
        Booking = booking ?? throw new ArgumentNullException(nameof(booking));
    }
}
