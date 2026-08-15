namespace Catalog.API.Application.Movies.Commands.DeleteMovie;

public class DeleteMovieCommandHandler : IRequestHandler<DeleteMovieCommand, bool>
{
    private readonly CatalogContext _context;
    
    public DeleteMovieCommandHandler(CatalogContext context)
    {
        _context = context;
    }   

    public async Task<bool> Handle(DeleteMovieCommand request, CancellationToken cancellationToken)
    {
        var movie = await _context.Movies.FindAsync([request.Id], cancellationToken);
        if (movie == null)
        {
            return false;
        }

        _context.Movies.Remove(movie);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}