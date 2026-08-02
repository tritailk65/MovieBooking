using BookingService.API.Application.IntegrationEvents;
using Catalog.API.Infrastucture;
using Catalog.API.IntegrationEvents;
using EventBus.Events;
using MassTransit;
using Seat.API.IntegrationEvents.EventHandlers;
using CatalogShowtimeCreated = Catalog.API.IntegrationEvents.Event.ShowtimeCreatedIntegrationEvent;
using SeatShowtimeCreated = Seat.API.IntegrationEvents.Events.ShowtimeCreatedIntegrationEvent;

namespace Booking.Saga.IntegrationTests;

public sealed class ShowtimeCreatedIntegrationEventConsumer(
    ShowtimeCreatedIntegrationEventHandler handler)
    : IConsumer<CatalogShowtimeCreated>
{
    public Task Consume(ConsumeContext<CatalogShowtimeCreated> context)
    {
        var message = context.Message;

        return handler.Handle(new SeatShowtimeCreated(
            message.ShowtimeId,
            message.HallId,
            message.MovieId,
            message.StartTime,
            message.EndTime,
            message.BasePrice,
            message.Seats));
    }
}

public sealed class TestCatalogIntegrationEventService(
    CatalogContext catalogContext,
    IPublishEndpoint publishEndpoint)
    : ICatalogIntegrationEventService
{
    public Task SaveEventAndCatalogContextChangesAsync(IntegrationEvent evt) =>
        catalogContext.SaveChangesAsync();

    public Task PublishThroughEventBusAsync(IntegrationEvent evt) =>
        publishEndpoint.Publish(evt, evt.GetType());
}

public sealed class TestBookingIntegrationEventService(IPublishEndpoint publishEndpoint)
    : IBookingIntegrationEventService
{
    public Task PublishEventsThroughEventBusAsync(Guid transactionId) =>
        Task.CompletedTask;

    public Task AddAndSaveEventAsync(IntegrationEvent evt) =>
        publishEndpoint.Publish(evt, evt.GetType());
}
