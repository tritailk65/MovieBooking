using EventBus.Abstractions;
using Microsoft.Extensions.Options;

namespace PaymentService.Api;

public class BookingStatusChangedToAwaitingPaymentIntegrationEventHandler (
    IEventBus eventBus,
    IOptionsMonitor<PaymentOptions> options,
    ILogger<BookingStatusChangedToAwaitingPaymentIntegrationEvent> logger
) : IIntegrationEventHandler<BookingStatusChangedToAwaitingPaymentIntegrationEvent>
{
    public async Task Handle (BookingStatusChangedToAwaitingPaymentIntegrationEvent @event)
    {
        logger.LogInformation("Handling integration event: {IntegrationEventId} - ({@IntegrationEvent})", @event.Id, @event);

        IntegrationEvent bookingPaymentIntegrationEvent;

        // Fake payment proccess
        if (options.CurrentValue.PaymentSucceeded)
        {
            await Task.Delay(10000);
            bookingPaymentIntegrationEvent = new BookingPaymentSucceededIntegrationEvent(@event.BookingId, @event.ShowtimeId, @event.BuyerId, @event.ReservationId);
        } else
        {
            await Task.Delay(3000);
            bookingPaymentIntegrationEvent = new BookingPaymentFailedIntegrationEvent(@event.BookingId, @event.ShowtimeId, @event.BuyerId, @event.ReservationId);
        }


        logger.LogInformation("Publishing integration event: {IntegrationEventId} - ({@IntegrationEvent})", bookingPaymentIntegrationEvent.Id, bookingPaymentIntegrationEvent);

        await eventBus.PublishAsync(bookingPaymentIntegrationEvent);

    }
}