namespace SagaOrchestration.Contracts;

public record SeatReservationConfirmedIntegrationEvent(
    Guid ReservationId,
    int BookingId) : IntegrationEvent;
