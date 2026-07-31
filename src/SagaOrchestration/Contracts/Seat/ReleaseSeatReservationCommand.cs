namespace SagaOrchestration.Contracts;

public record ReleaseSeatReservationCommand(
    Guid ReservationId,
    int BookingId,
    int ShowtimeId,
    string UserId);
