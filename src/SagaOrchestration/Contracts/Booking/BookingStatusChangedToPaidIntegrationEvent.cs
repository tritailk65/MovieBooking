namespace SagaOrchestration.Contracts;

public record BookingStatusChangedToPaidIntegrationEvent(
    Guid ReservationId,
    int BookingId) : IntegrationEvent;
