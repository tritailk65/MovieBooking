namespace Seat.API.Application.Seats.GetSeatReservation;

public record GetSeatReservationQuery: IRequest<SeatReservation>
{
    public int ShowtimeId {get;set;}
    public string UserId {get; set;}

    public GetSeatReservationQuery(int showtimeId, string userId)
    {
        ShowtimeId = showtimeId;
        UserId = userId;
    }
}