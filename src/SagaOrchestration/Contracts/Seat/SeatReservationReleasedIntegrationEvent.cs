namespace SagaOrchestration.Contracts;

public record SeatReservationReleasedIntegrationEvent(
    Guid ReservationId,
    int BookingId) : IntegrationEvent;
