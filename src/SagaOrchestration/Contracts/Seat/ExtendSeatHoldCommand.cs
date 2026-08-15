namespace SagaOrchestration.Contracts;

public record ExtendSeatHoldCommand(
    Guid ReservationId,
    int BookingId,
    int ShowtimeId,
    string UserId);
