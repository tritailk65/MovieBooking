namespace SagaOrchestration.Contracts;

public record BookingCancellationRequestedIntegrationEvent(
    Guid ReservationId,
    int BookingId) : IntegrationEvent;