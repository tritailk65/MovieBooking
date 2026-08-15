namespace Catalog.APi.Application.Movies.Queries.GetMovies;

public record MovieDto
(
    int Id,
    string Title,
    string Description,
    int DurationMinutes,
    DateTime ReleaseDate,
    string PosterUrl,
    string TrailerUrl,
    string Director,
    string Cast,
    bool IsShowing
);