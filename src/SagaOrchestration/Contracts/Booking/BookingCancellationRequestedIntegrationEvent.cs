namespace SagaOrchestration.Contracts;

public record BookingCancellationRequestedIntegrationEvent(
    Guid ReservationId,
    int BookingId,
    string UserId,
    int ShowtimeId) : IntegrationEvent;