namespace Catalog.API.Application.Showtimes.Queries.GetShowtimes;

public sealed record ShowtimeDto(
    int Id,
    int MovieId,
    string MovieTitle,
    int CinemaId,
    string CinemaName,
    string CinemaAddress,
    string CinemaCity,
    int HallId,
    string HallName,
    DateTime StartTime,
    DateTime EndTime,
    decimal BasePrice);
