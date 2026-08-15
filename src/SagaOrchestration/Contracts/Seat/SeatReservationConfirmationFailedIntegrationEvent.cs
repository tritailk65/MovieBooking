namespace SagaOrchestration.Contracts;

public record SeatReservationConfirmationFailedIntegrationEvent(
    Guid ReservationId,
    int BookingId,
    string Reason) : IntegrationEvent;