namespace SagaOrchestration.Contracts;

public record SeatReservationFailedIntegrationEvent(
    Guid ReservationId,
    int BookingId,
    string Reason) : IntegrationEvent;