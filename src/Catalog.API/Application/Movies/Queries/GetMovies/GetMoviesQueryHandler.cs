using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace Catalog.APi.Application.Movies.Queries.GetMovies;

public class GetMoviesQueryHandler : IRequestHandler<GetMoviesQuery, PaginatedResult<MovieDto>>
{
    private readonly CatalogContext _context;
    private readonly IDistributedCache _cache;

    public GetMoviesQueryHandler(CatalogContext context, IDistributedCache cache)
    {
        _context = context;
        _cache = cache;
    }

    public async Task<PaginatedResult<MovieDto>> Handle(GetMoviesQuery request, CancellationToken cancellationToken)
    {
        using var activity = ActivityExtensions.ActivitySource.StartActivity("catalog.movie.get");
        activity?.SetTag("catalog.movie.page_index", request.PageIndex);
        activity?.SetTag("catalog.movie.page_size", request.PageSize);

        try
        {           
            string cacheKey = $"Movies_Page_{request.PageIndex}_Size_{request.PageSize}";
            var cachedData = await _cache.GetStringAsync(cacheKey, cancellationToken);
            if (!string.IsNullOrEmpty(cachedData))
            {
                activity?.SetTag("cache.hit", true);
                return JsonSerializer.Deserialize<PaginatedResult<MovieDto>>(cachedData);
            }

            activity?.SetTag("cache.hit", false);

            var query = _context.Movies.AsNoTracking();

            var totalItems = await _context.Movies.CountAsync(cancellationToken);

            var movies = await _context.Movies
                .AsNoTracking()
                .OrderByDescending(m => m.ReleaseDate)
                .Skip((request.PageIndex - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(m => new MovieDto(m.Id, m.Title, m.Description, m.DurationMinutes, m.ReleaseDate, m.PosterUrl, m.TrailerUrl, m.Director, m.Cast, m.IsShowing))
                .ToListAsync(cancellationToken);

            var result = new PaginatedResult<MovieDto>(request.PageIndex, request.PageSize, totalItems, movies);

            activity?.SetTag("catalog.movie.result_count", movies.Count);
            activity?.SetTag("catalog.movie.total_count", totalItems);

            // Lưu kết quả vào Redis Cache 
            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10),
                SlidingExpiration = TimeSpan.FromMinutes(2) // Xoá sau 2 giây
            };

            // TODO: Khi gọi command CRUD nên set lại dữ liệu của cache
            var serializedResult = JsonSerializer.Serialize(result);
            await _cache.SetStringAsync(cacheKey, serializedResult, cacheOptions, cancellationToken);

            return new PaginatedResult<MovieDto>(request.PageIndex, request.PageSize, totalItems, movies);
        } catch (Exception ex)
        {
            activity?.SetExceptionTags(ex);
            throw;
        }


    }
}