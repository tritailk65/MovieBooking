namespace SagaOrchestration.Contracts;

public record RequestPaymentCommand(
    Guid ReservationId,
    int BookingId,
    string UserId,
    decimal Amount);