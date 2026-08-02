namespace Seat.API.IntegrationEvents.Consumer;

using MassTransit;
using SagaOrchestration.Contracts;
using Seat.API.IntegrationEvents.EventHandlers;

public sealed class ShowtimeCreatedIntegrationEventConsumer(
    ShowtimeCreatedIntegrationEventHandler handler)
    : IConsumer<ShowtimeCreatedIntegrationEvent>
{
    public Task Consume(
        ConsumeContext<ShowtimeCreatedIntegrationEvent> context)
    {
        return handler.Handle(context.Message);
    }
}
