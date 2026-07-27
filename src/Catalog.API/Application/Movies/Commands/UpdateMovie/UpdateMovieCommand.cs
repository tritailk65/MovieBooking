namespace Catalog.API.Application.Movies.Commands.UpdateMovie;

public record UpdateMovieCommand(
    int Id,
    string Title,
    string Description,
    int DurationMinutes,
    DateTime ReleaseDate,
    string Director,
    string Cast,
    string TrailerUrl,
    string PosterUrl
) : IRequest<bool>; // Trả về true / false