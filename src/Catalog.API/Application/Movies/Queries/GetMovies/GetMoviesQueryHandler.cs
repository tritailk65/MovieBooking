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
        string cacheKey = $"Movies_Page_{request.PageIndex}_Size_{request.PageSize}";
        var cachedData = await _cache.GetStringAsync(cacheKey, cancellationToken);
        if (!string.IsNullOrEmpty(cachedData))
        {
            return JsonSerializer.Deserialize<PaginatedResult<MovieDto>>(cachedData);
        }

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
    }
}