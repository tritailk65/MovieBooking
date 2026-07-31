namespace SagaOrchestration.Contracts;

public record PaymentTimedOutIntegrationEvent(
    Guid ReservationId,
    int BookingId) : IntegrationEvent;