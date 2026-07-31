namespace SagaOrchestration.Contracts;

public record ReserveSeatsCommand(
    Guid ReservationId,
    int BookingId,
    int ShowtimeId,
    string UserId,
    int ReservationVersion);
