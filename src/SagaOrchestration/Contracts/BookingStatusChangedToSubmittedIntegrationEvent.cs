namespace SagaOrchestration.Contracts;

public record BookingStatusChangedToSubmittedIntegrationEvent(
    Guid ReservationId,
    int BookingId,
    int ShowtimeId,
    string UserId,
    IReadOnlyCollection<string> SeatIds,
    decimal TotalPrice,
    int ReservationVersion,
    DateTime PreparedUntil) : IntegrationEvent;