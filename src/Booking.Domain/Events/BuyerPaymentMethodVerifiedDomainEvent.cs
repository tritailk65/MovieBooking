using BookingService.Domain.AggregateModel.BuyerAggregate;
using MediatR;

namespace BookingService.Domain.Events;

public class BuyerPaymentMethodVerifiedDomainEvent : INotification
{
    public Buyer Buyer { get; }
    public PaymentMethod Payment { get; }
    public int BookingId { get; }

    public BuyerPaymentMethodVerifiedDomainEvent(Buyer buyer, PaymentMethod payment, int bookingId)
    {
        Buyer = buyer ?? throw new ArgumentNullException(nameof(buyer));
        Payment = payment ?? throw new ArgumentNullException(nameof(payment));
        BookingId = bookingId;
    }
}
