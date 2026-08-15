using BookingService.API.Application.IntegrationEvents;
using Catalog.API.Infrastucture;
using Catalog.API.IntegrationEvents;
using EventBus.Events;
using MassTransit;

namespace Booking.Saga.IntegrationTests;

// Old test-only adapter converted the Catalog-local contract into the
// Seat-local contract. Catalog and Seat now publish/consume the shared
// SagaOrchestration.Contracts.ShowtimeCreatedIntegrationEvent directly.
// public sealed class ShowtimeCreatedIntegrationEventConsumer(...)

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
