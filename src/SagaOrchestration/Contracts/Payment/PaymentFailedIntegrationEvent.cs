namespace SagaOrchestration.Contracts;

public record PaymentFailedIntegrationEvent(
    Guid ReservationId,
    int BookingId,
    string Reason) : IntegrationEvent;