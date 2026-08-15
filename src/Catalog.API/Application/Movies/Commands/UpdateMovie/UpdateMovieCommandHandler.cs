namespace Catalog.API.Application.Movies.Commands.UpdateMovie;

public class UpdateMovieCommandHandler : IRequestHandler<UpdateMovieCommand, bool>
{
    private readonly CatalogContext _context;
    
    public UpdateMovieCommandHandler(CatalogContext context)
    {
        _context = context;
    }   

    public async Task<bool> Handle(UpdateMovieCommand request, CancellationToken cancellationToken)
    {
        var movie = await _context.Movies.FindAsync([request.Id], cancellationToken);
        if (movie == null)
        {
            return false;
        }

        movie.Title = request.Title;
        movie.Description = request.Description;
        movie.DurationMinutes = request.DurationMinutes;
        movie.ReleaseDate = request.ReleaseDate;
        movie.Director = request.Director;
        movie.Cast = request.Cast;
        movie.TrailerUrl = request.TrailerUrl;
        movie.PosterUrl = request.PosterUrl;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}