namespace SagaOrchestration.Contracts;

public record  PaymentProviderConfirmedUnpaidIntegrationEvent(
    Guid ReservationId,
    int BookingId,
    string UserId,
    string PaymentId,
    decimal Amount) : IntegrationEvent;
