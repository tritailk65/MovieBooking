namespace SagaOrchestration.Contracts;

public record CancelBookingCommand(
    Guid ReservationId,
    int BookingId,
    string? Reason);