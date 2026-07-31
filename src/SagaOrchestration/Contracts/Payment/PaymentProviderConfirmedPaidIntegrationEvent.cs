namespace SagaOrchestration.Contracts;

public record PaymentProviderConfirmedPaidIntegrationEvent(
    Guid ReservationId,
    int BookingId,
    string UserId,
    string PaymentId,
    decimal Amount) : IntegrationEvent;
