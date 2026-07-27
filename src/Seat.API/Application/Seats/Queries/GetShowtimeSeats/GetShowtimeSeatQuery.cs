namespace Seat.API.Application.Seats.GetShowtimeSeats;

public class GetShowtimeSeatQuery : IRequest<ShowtimeSeat>
{
    public int ShowtimeId {get; set;}

    public GetShowtimeSeatQuery(int showtimeId)
    {
        ShowtimeId = showtimeId;
    }
}