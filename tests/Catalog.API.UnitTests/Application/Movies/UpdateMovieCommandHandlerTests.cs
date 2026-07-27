using Catalog.API.Application.Movies.Commands.UpdateMovie;
using Catalog.API.Domain.Entities;
using Catalog.API.UnitTests.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace Catalog.API.UnitTests.Application.Movies;


public class UpdateMovieCommandHandlerTests
{
    /// <summary>
    /// UpdateMovieCommandHandler -> cập nhật Movie
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task Handle_WhenMovieExists_ShouldUpdateMovie()
    {
        await using var context = CatalogContextFactory.Create();

        var movie = new Movie
        {
            Title = "Old title",
            Description = "Old description",
            DurationMinutes = 100,
            ReleaseDate = new DateTime(2024, 1, 1),
            Director = "Old director",
            Cast = "Old cast",
            TrailerUrl = "old-trailer",
            PosterUrl = "old-poster"
        };

        context.Movies.Add(movie);
        await context.SaveChangesAsync();

        var handler = new UpdateMovieCommandHandler(context);

        var command = new UpdateMovieCommand(
            Id: movie.Id,
            Title: "New title",
            Description: "New description",
            DurationMinutes: 120,
            ReleaseDate: new DateTime(2024, 2, 1),
            Director: "New director",
            Cast: "New cast",
            TrailerUrl: "new-trailer",
            PosterUrl: "new-poster"
        );

        var result = await handler.Handle(command, CancellationToken.None);

        var updatedMovie = await context.Movies.SingleAsync(m => m.Id == movie.Id);

        Assert.True(result);
        Assert.Equal("New title", updatedMovie.Title);
        Assert.Equal("New description", updatedMovie.Description);
        Assert.Equal(120, updatedMovie.DurationMinutes);
        Assert.Equal("New director", updatedMovie.Director);
        Assert.Equal("New cast", updatedMovie.Cast);
        Assert.Equal("new-trailer", updatedMovie.TrailerUrl);
        Assert.Equal("new-poster", updatedMovie.PosterUrl);
    }
}