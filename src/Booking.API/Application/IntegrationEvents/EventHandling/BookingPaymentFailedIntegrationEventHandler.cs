using BookingService.API.Application.Commands.CancelBooking;

namespace BookingService.API.Application.IntegrationEvents.EventHandling;

public class BookingPaymentFailedIntegrationEventHandler(
    IMediator mediator,
    ILogger<BookingPaymentFailedIntegrationEventHandler> logger
) : IIntegrationEventHandler<BookingPaymentFailedIntegrationEvent>
{
    public Task Handle(BookingPaymentFailedIntegrationEvent @event)
    {
        logger.LogInformation("Handling integration event: {IntegrationEventId} - ({@IntegrationEvent})", @event.Id, @event);

        var command = new CancelBookingCommand(@event.BookingId);

        logger.LogInformation(
            "Sending command: {CommandName} - {IdProperty}: {CommandId} ({@Command})",
            command.GetGenericTypeName(),
            nameof(command.bookingId),
            command.bookingId,
            command);

        return mediator.Send(command);
    }   
}
