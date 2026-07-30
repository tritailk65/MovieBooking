
using BookingService.API.Application.Commands.CancelBooking;
using SagaOrchestration.Contract;
using CancelBookingApplicationCommand = BookingService.API.Application.Commands.CancelBooking.CancelBookingCommand;

namespace BookingService.API.Application.IntegrationEvents.EventHandling;


public class BookingCancelIntegrationEventHandler(
    IMediator mediator,
    ILogger<BookingCancelIntegrationEventHandler> logger
) : IIntegrationEventHandler<BookingCancelIntegrationEvent>
{
    public Task Handle(BookingCancelIntegrationEvent @event)
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
