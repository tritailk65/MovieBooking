namespace Catalog.API.Application.Showtimes.Queries.GetShowtimes;

public sealed class GetShowtimesQueryHandler(CatalogContext context)
    : IRequestHandler<GetShowtimesQuery, IReadOnlyCollection<ShowtimeDto>>
{
    public async Task<IReadOnlyCollection<ShowtimeDto>> Handle(
        GetShowtimesQuery request,
        CancellationToken cancellationToken)
    {
        var showtimes = context.Showtimes.AsNoTracking();

        if (request.MovieId.HasValue)
        {
            showtimes = showtimes.Where(showtime =>
                showtime.MovieId == request.MovieId.Value);
        }

        if (request.CinemaId.HasValue)
        {
            showtimes = showtimes.Where(showtime =>
                showtime.CinemaId == request.CinemaId.Value);
        }

        if (request.Date.HasValue)
        {
            var startOfDay = DateTime.SpecifyKind(
                request.Date.Value.ToDateTime(TimeOnly.MinValue),
                DateTimeKind.Utc);
            var endOfDay = startOfDay.AddDays(1);

            showtimes = showtimes.Where(showtime =>
                showtime.StartTime >= startOfDay &&
                showtime.StartTime < endOfDay);
        }

        return await (
                from showtime in showtimes
                join movie in context.Movies.AsNoTracking()
                    on showtime.MovieId equals movie.Id
                join cinema in context.Cinemas.AsNoTracking()
                    on showtime.CinemaId equals cinema.Id
                join hall in context.Halls.AsNoTracking()
                    on showtime.HallId equals hall.Id
                orderby showtime.StartTime
                select new ShowtimeDto(
                    showtime.Id,
                    movie.Id,
                    movie.Title,
                    cinema.Id,
                    cinema.Name,
                    cinema.Address,
                    cinema.City,
                    hall.Id,
                    hall.Name,
                    showtime.StartTime,
                    showtime.EndTime,
                    showtime.BasePrice))
            .ToListAsync(cancellationToken);
    }
}
