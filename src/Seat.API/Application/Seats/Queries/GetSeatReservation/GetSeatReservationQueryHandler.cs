namespace Seat.API.Application.Seats.GetSeatReservation;

public class GetSeatReservationQueryHandler : IRequestHandler<GetSeatReservationQuery, SeatReservation>
{
    private readonly ISeatRepository _seatRepository;
    public GetSeatReservationQueryHandler(ISeatRepository seatRepository) => _seatRepository = seatRepository;

    public Task<SeatReservation> Handle (GetSeatReservationQuery query , CancellationToken cancellationToken)
    {
        var reservation = _seatRepository.GetSeatReservationHashAsync(query.ShowtimeId, query.UserId);
        if (reservation is null) return null;
        return reservation;
    }
}