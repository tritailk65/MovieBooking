namespace Seat.API.Application.Seats.GetShowtimeSeats;

public class SeatDto
{
    public int Id {get; set;}
    public int ShowtimeId {get; set;}
    public DateTime ServertimeUtc {get; set;}  
    public IEnumerable<string> Seats {get; set;}
}