using EventBus.Events;

namespace Seat.API.IntegrationEvents.Events;

public record BookingStartedIntegrationEvent(string userId) : IntegrationEvent;