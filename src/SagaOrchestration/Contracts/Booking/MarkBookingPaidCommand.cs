namespace SagaOrchestration.Contracts;

public record MarkBookingPaidCommand(
    Guid ReservationId,
    int BookingId,
    string? PaymentId);