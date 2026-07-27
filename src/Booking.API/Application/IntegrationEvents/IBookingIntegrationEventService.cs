
namespace BookingService.API.Application.IntegrationEvents;

using EventBus.Events;

public interface IBookingIntegrationEventService
{
    Task PublishEventsThroughEventBusAsync(Guid transactionId);
    Task AddAndSaveEventAsync(IntegrationEvent evt);
}