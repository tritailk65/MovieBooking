namespace SagaOrchestration.Contracts;

public record SeatHoldExtensionFailedIntegrationEvent(
    Guid ReservationId,
    int BookingId,
    string Reason) : IntegrationEvent;