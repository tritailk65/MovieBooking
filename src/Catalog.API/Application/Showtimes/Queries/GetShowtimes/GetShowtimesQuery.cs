namespace Catalog.API.Application.Showtimes.Queries.GetShowtimes;

public sealed record GetShowtimesQuery(
    int? MovieId = null,
    int? CinemaId = null,
    DateOnly? Date = null) : IRequest<IReadOnlyCollection<ShowtimeDto>>;
