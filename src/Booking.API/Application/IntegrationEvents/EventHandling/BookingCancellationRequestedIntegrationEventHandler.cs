
using SagaOrchestration.Contracts;
using CancelBookingApplicationCommand = BookingService.API.Application.Commands.CancelBooking.CancelBookingCommand;

namespace BookingService.API.Application.IntegrationEvents.EventHandling;


public class BookingCancellationRequestedIntegrationEventHandler(
    IMediator mediator,
    ILogger<BookingCancellationRequestedIntegrationEventHandler> logger
) : IIntegrationEventHandler<BookingCancellationRequestedIntegrationEvent>
{
    public Task Handle(BookingCancellationRequestedIntegrationEvent @event)
    {
        logger.LogInformation("Handling integration event: {IntegrationEventId} - ({@IntegrationEvent})", @event.Id, @event);

        var command = new CancelBookingApplicationCommand(@event.BookingId);

        logger.LogInformation(
            "Sending command: {CommandName} - {IdProperty}: {CommandId} ({@Command})",
            command.GetGenericTypeName(),
            nameof(command.bookingId),
            command.bookingId,
            command);

        return mediator.Send(command);
    }   
}
