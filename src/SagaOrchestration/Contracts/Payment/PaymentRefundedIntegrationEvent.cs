namespace SagaOrchestration.Contracts;

public record PaymentRefundedIntegrationEvent(
    Guid ReservationId,
    int BookingId,
    string? PaymentId) : IntegrationEvent;