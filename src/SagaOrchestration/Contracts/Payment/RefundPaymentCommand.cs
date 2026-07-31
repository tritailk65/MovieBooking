namespace SagaOrchestration.Contracts;

public record RefundPaymentCommand(
    Guid ReservationId,
    int BookingId,
    string? PaymentId,
    decimal Amount,
    string? Reason);