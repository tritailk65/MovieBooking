namespace SagaOrchestration.Contracts;

public record BookingPendingPaymentExpiredIntegrationEvent(
    Guid ReservationId,
    int BookingId) : IntegrationEvent;