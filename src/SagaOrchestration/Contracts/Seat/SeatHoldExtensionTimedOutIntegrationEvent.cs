namespace SagaOrchestration.Contracts;

public record SeatHoldExtensionTimedOutIntegrationEvent(
    Guid ReservationId,
    int BookingId,
    string Reason) : IntegrationEvent;