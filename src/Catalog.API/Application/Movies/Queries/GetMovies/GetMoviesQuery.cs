 namespace Catalog.APi.Application.Movies.Queries.GetMovies;

public record GetMoviesQuery(int PageIndex = 1, int PageSize = 10) : IRequest<PaginatedResult<MovieDto>>;
