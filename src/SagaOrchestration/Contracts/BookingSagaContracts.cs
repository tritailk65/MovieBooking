namespace SagaOrchestration.Contract;

// Placeholder contracts for the orchestration prototype.
// Move these records to a shared contracts project before wiring production services.

public record BookingSubmittedIntegrationEvent(
    Guid ReservationId,
    int BookingId,
    int ShowtimeId,
    string UserId,
    IReadOnlyCollection<string> SeatIds,
    decimal TotalPrice,
    int ReservationVersion,
    DateTime PreparedUntil) : IntegrationEvent;

public record RequestPaymentCommand(
    Guid ReservationId,
    int BookingId,
    string UserId,
    decimal Amount);

public record PaymentSucceededIntegrationEvent(
    Guid ReservationId,
    int BookingId,
    string PaymentId) : IntegrationEvent;

public record PaymentFailedIntegrationEvent(
    Guid ReservationId,
    int BookingId,
    string Reason) : IntegrationEvent;

public record PaymentTimedOutIntegrationEvent(
    Guid ReservationId,
    int BookingId) : IntegrationEvent;

public record ConfirmSeatReservationCommand(
    Guid ReservationId,
    int BookingId,
    int ShowtimeId,
    string UserId,
    int ReservationVersion);

public record SeatReservationConfirmedIntegrationEvent(
    Guid ReservationId,
    int BookingId) : IntegrationEvent;

public record SeatReservationConfirmationFailedIntegrationEvent(
    Guid ReservationId,
    int BookingId,
    string Reason) : IntegrationEvent;

public record SeatReservationConfirmationTimedOutIntegrationEvent(
    Guid ReservationId,
    int BookingId) : IntegrationEvent;

public record MarkBookingPaidCommand(
    Guid ReservationId,
    int BookingId,
    string? PaymentId);

public record BookingMarkedPaidIntegrationEvent(
    Guid ReservationId,
    int BookingId) : IntegrationEvent;

public record CancelBookingCommand(
    Guid ReservationId,
    int BookingId,
    string? Reason);

public record BookingCancelledIntegrationEvent(
    Guid ReservationId,
    int BookingId) : IntegrationEvent;

public record ReleaseSeatReservationCommand(
    Guid ReservationId,
    int BookingId,
    int ShowtimeId,
    string UserId);

public record SeatReservationReleasedIntegrationEvent(
    Guid ReservationId,
    int BookingId) : IntegrationEvent;

public record RefundPaymentCommand(
    Guid ReservationId,
    int BookingId,
    string? PaymentId,
    decimal Amount,
    string? Reason);

public record PaymentRefundedIntegrationEvent(
    Guid ReservationId,
    int BookingId,
    string? PaymentId) : IntegrationEvent;
