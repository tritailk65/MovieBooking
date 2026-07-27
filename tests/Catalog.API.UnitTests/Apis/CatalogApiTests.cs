using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using global::Catalog.API.Application.Movies.Commands.UpdateMovie;
using global::Catalog.API.Application.Moviess.Commands.CreateMovie;
using global::Catalog.API.Application.Showtimes.Commands.CreateShowtime;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Catalog.API.UnitTests.Apis;

public class CatalogApiTests
{
    /// <summary>
    /// POST /movies -> Created
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task CreateMovie_WhenMediatorReturnsMovieId_ShouldReturnCreated()
    {
        var mediator = Substitute.For<IMediator>();

        mediator.Send(Arg.Any<CreateMovieCommand>(), Arg.Any<CancellationToken>())
            .Returns(123);

        await using var app = await CreateTestAppAsync(mediator);
        var client = app.GetTestClient();

        var request = new CreateMovieCommand(
            Tiltle: "Dune Part Two",
            Description: "Epic sci-fi movie",
            DurationMinutes: 166,
            ReleaseDate: new DateTime(2024, 3, 1),
            Director: "Denis Villeneuve",
            Cast: "Timothee Chalamet, Zendaya",
            TrailerUrl: "https://example.com/trailer",
            PosterUrl: "https://example.com/poster"
        );

        var response = await client.PostAsJsonAsync("/api/v1/catalog/movies", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        await mediator.Received(1)
            .Send(Arg.Any<CreateMovieCommand>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// PUT /{id} -> NoContent
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task UpdateMovie_WhenIdMatchesAndMediatorReturnsTrue_ShouldReturnNoContent()
    {
        var mediator = Substitute.For<IMediator>();

        mediator.Send(Arg.Any<UpdateMovieCommand>(), Arg.Any<CancellationToken>())
            .Returns(true);

        await using var app = await CreateTestAppAsync(mediator);
        var client = app.GetTestClient();

        var request = new UpdateMovieCommand(
            Id: 10,
            Title: "Updated movie",
            Description: "Updated description",
            DurationMinutes: 120,
            ReleaseDate: new DateTime(2024, 4, 1),
            Director: "Director",
            Cast: "Cast",
            TrailerUrl: "trailer",
            PosterUrl: "poster"
        );

        var response = await client.PutAsJsonAsync("/api/v1/catalog/10", request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        await mediator.Received(1)
            .Send(Arg.Any<UpdateMovieCommand>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// POST /showtimes -> Created
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task UpdateMovie_WhenRouteIdDoesNotMatchBodyId_ShouldReturnBadRequest()
    {
        var mediator = Substitute.For<IMediator>();

        await using var app = await CreateTestAppAsync(mediator);
        var client = app.GetTestClient();

        var request = new UpdateMovieCommand(
            Id: 99,
            Title: "Updated movie",
            Description: "Updated description",
            DurationMinutes: 120,
            ReleaseDate: new DateTime(2024, 4, 1),
            Director: "Director",
            Cast: "Cast",
            TrailerUrl: "trailer",
            PosterUrl: "poster"
        );

        var response = await client.PutAsJsonAsync("/api/v1/catalog/10", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        await mediator.DidNotReceive()
            .Send(Arg.Any<UpdateMovieCommand>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// CreateMovieCommandHandler -> lưu Movie
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task CreateShowtime_WhenMediatorReturnsShowtimeId_ShouldReturnCreated()
    {
        var mediator = Substitute.For<IMediator>();

        mediator.Send(Arg.Any<CreateShowtimeCommand>(), Arg.Any<CancellationToken>())
            .Returns(456);

        await using var app = await CreateTestAppAsync(mediator);
        var client = app.GetTestClient();

        var request = new CreateShowtimeCommand(
            MovieId: 1,
            CinemaId: 2,
            HallId: 3,
            StartTime: new DateTime(2026, 1, 1, 18, 0, 0),
            EndTime: new DateTime(2026, 1, 1, 20, 0, 0),
            BasePrice: 90000m
        );

        var response = await client.PostAsJsonAsync("/api/v1/catalog/showtimes", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        await mediator.Received(1)
            .Send(Arg.Any<CreateShowtimeCommand>(), Arg.Any<CancellationToken>());
    }

    private static async Task<WebApplication> CreateTestAppAsync(IMediator mediator)
    {
        var builder = WebApplication.CreateBuilder();

        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders();

        builder.Services.AddRouting();
        builder.Services.AddSingleton(mediator);
        builder.Services
            .AddAuthentication(TestAuthenticationHandler.AuthenticationSchemeName)
            .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(
                TestAuthenticationHandler.AuthenticationSchemeName,
                _ => { });
        builder.Services.AddCatalogAuthorization();

        var app = builder.Build();

        app.UseAuthentication();
        app.UseAuthorization();
        app.MapCatalogApi();

        await app.StartAsync();

        return app;
    }

    private sealed class TestAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        public const string AuthenticationSchemeName = "Test";

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            Claim[] claims =
            [
                new(ClaimTypes.NameIdentifier, "catalog-api-test-user"),
                new("scope", $"{CatalogScopes.Read} {CatalogScopes.Write}")
            ];

            var identity = new ClaimsIdentity(claims, AuthenticationSchemeName);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, AuthenticationSchemeName);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
