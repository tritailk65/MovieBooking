namespace Catalog.API.Application.Moviess.Commands.CreateMovie;

public record CreateMovieCommand (
    string Tiltle,
    string Description,
    int DurationMinutes,
    DateTime ReleaseDate,
    string Director,
    string Cast,
    string TrailerUrl,
    string PosterUrl
) : IRequest<int>;
