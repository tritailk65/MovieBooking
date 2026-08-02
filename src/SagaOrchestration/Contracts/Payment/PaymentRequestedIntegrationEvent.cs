namespace SagaOrchestration.Contracts;

public record PaymentRequestedIntegrationEvent(
    Guid ReservationId,
    int BookingId,
    string UserId,
    decimal Amount) : IntegrationEvent;