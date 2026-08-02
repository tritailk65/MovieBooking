namespace Seat.API.Application.Seats.Command.ExtendSeatHold;

public record ExtendSeatHoldApplicationCommand(
    Guid ReservationId,
    int BookingId,
    int ShowtimeId,
    string UserId,
    TimeSpan ExtensionDuration): IRequest<ExtendSeatHoldResult>;


public sealed record ExtendSeatHoldResult(
    bool Succeeded,
    string Reason = "");
