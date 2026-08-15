namespace SagaOrchestration.Contracts;

public record SeatHoldExtendedIntegrationEvent(
    Guid ReservationId,
    int BookingId) : IntegrationEvent;