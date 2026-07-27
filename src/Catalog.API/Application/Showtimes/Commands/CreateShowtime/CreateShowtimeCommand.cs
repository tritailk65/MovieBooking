namespace Catalog.API.Application.Showtimes.Commands.CreateShowtime;

public record CreateShowtimeCommand(
    int MovieId, 
    int CinemaId, 
    int HallId, 
    DateTime StartTime, 
    DateTime EndTime, 
    decimal BasePrice) : IRequest<int>;