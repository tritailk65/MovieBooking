namespace Seat.API.IntegrationEvents.EventHandlers;

using EventBus.Abstractions;
using Seat.API.Domain.Interfaces;
using Seat.API.IntegrationEvents.Events;

public class BookingStartedIntegrationEventhandler(
    ISeatRepository seatRepository,
    ILogger<BookingStartedIntegrationEventhandler> logger) : IIntegrationEventHandler<BookingStartedIntegrationEvent>

{
    public async Task Handle(BookingStartedIntegrationEvent @event)
    {
        logger.LogInformation("Handling integration event: {IntegrationEventId} - ({@IntegrationEvent})", @event.Id, @event);

        // Them 1 ham xoa bo lock ghe dang giu cua user
    }
}