namespace SagaOrchestration.Contracts;

public record SeatReservationHeldIntegrationEvent(
    Guid ReservationId,
    int BookingId) : IntegrationEvent;