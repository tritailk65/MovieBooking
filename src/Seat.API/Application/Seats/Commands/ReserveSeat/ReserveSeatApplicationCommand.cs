namespace Seat.API.Application.Seats.Command.ReserveSeat;

public sealed record ReserveSeatApplicationCommand(
    Guid ReservationId,
    int BookingId,
    int ShowtimeId,
    string UserId,
    int ReservationVersion)
    : IRequest<ReserveSeatsResult>;

public sealed record ReserveSeatsResult(
    bool Succeeded,
    string Reason = "");