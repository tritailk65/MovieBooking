namespace SagaOrchestration.Contracts;

public record SeatReservationTimedOutIntegrationEvent(
    Guid ReservationId,
    int BookingId) : IntegrationEvent;