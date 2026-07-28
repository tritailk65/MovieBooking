using Catalog.API.Application.Movies.Commands.CreateMovie;
using Catalog.API.Application.Moviess.Commands.CreateMovie;
using Catalog.API.UnitTests.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace Catalog.API.UnitTests.Application.Movies;

public class CreateMovieCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenCommandIsValid_ShouldCreateMovie()
    {
        await using var context = CatalogContextFactory.Create();

        var handler = new CreateMovieCommandHandler(context);

        var command = new CreateMovieCommand(
            Tiltle: "Dune Part Two",
            Description: "Epic sci-fi movie",
            DurationMinutes: 166,
            ReleaseDate: new DateTime(2024, 3, 1),
            Director: "Denis Villeneuve",
            Cast: "Timothee Chalamet, Zendaya",
            TrailerUrl: "https://example.com/trailer",
            PosterUrl: "https://example.com/poster"
        );

        var movieId = await handler.Handle(command, CancellationToken.None);

        var movie = await context.Movies.SingleAsync(m => m.Id == movieId);

        Assert.Equal("Dune Part Two", movie.Title);
        Assert.Equal("Epic sci-fi movie", movie.Description);
        Assert.Equal(166, movie.DurationMinutes);
        Assert.Equal("Denis Villeneuve", movie.Director);
        Assert.Equal("Timothee Chalamet, Zendaya", movie.Cast);
        Assert.Equal("https://example.com/trailer", movie.TrailerUrl);
        Assert.Equal("https://example.com/poster", movie.PosterUrl);
    }
}