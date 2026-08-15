namespace SagaOrchestration.Contracts;

public record PaymentSucceededIntegrationEvent(
    Guid ReservationId,
    int BookingId,
    string PaymentId) : IntegrationEvent;