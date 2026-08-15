namespace SagaOrchestration.Contracts;


public record BookingStatusChangedToCancelledIntegrationEvent(
    Guid ReservationId,
    int BookingId) : IntegrationEvent;