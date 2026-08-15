namespace SagaOrchestration.Contracts;

public record MarkBookingExpiredCommand(
    Guid ReservationId,
    int BookingId);