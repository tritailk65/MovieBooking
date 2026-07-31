namespace SagaOrchestration.Contracts;

public record SeatHoldDeadlineReachedIntegrationEvent(
    Guid ReservationId,
    int BookingId) : IntegrationEvent;