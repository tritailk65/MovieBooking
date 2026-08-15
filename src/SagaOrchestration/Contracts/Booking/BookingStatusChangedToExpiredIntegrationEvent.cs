namespace SagaOrchestration.Contracts;

public record BookingStatusChangedToExpiredIntegrationEvent(
    Guid ReservationId,
    int BookingId) : IntegrationEvent;
