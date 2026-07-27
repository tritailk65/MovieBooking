namespace BookingService.API.Application.IntegrationEvents.EventHandling;

public class BookingPaymentSucceedIntegrationEventHandler(
    IMediator mediator,
    ILogger<BookingPaymentSucceedIntegrationEventHandler> logger
) : IIntegrationEventHandler<BookingPaymentSucceededIntegrationEvent>
{
    public Task Handle(BookingPaymentSucceededIntegrationEvent @event)
    {
        logger.LogInformation("Handling integration event: {IntegrationEventId} - ({@IntegrationEvent})", @event.Id, @event);

        var command = new SetPaidBookingStatusCommand(@event.BookingId);

        logger.LogInformation(
            "Sending command: {CommandName} - {IdProperty}: {CommandId} ({@Command})",
            command.GetGenericTypeName(),
            nameof(command.bookingId),
            command.bookingId,
            command);

        return mediator.Send(command);
    }   
}