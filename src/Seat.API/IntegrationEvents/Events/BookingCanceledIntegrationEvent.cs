using EventBus.Events;

namespace Seat.API.IntegrationEvents.Events;

public record BookingCanceledIntegrationEvent(int showtimeId, string userId) : IntegrationEvent;