namespace SagaOrchestration.Contracts;

public record ConfirmSeatReservationCommand(
    Guid ReservationId,
    int BookingId,
    int ShowtimeId,
    string UserId,
    int ReservationVersion);

