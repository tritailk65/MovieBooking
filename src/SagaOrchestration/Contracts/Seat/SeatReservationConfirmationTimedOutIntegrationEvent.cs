namespace SagaOrchestration.Contracts;

public record SeatReservationConfirmationTimedOutIntegrationEvent(
    Guid ReservationId,
    int BookingId) : IntegrationEvent;
