namespace SagaOrchestration.Contracts;

public record LatePaymentSuccededIntegrationEvent(
    Guid ReservationId,
    int BookingId,
    string PaymentId) : IntegrationEvent;