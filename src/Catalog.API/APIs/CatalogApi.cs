using ServiceDefaults.Authorization;
using Catalog.API.Application.Showtimes.Queries.GetShowtimes;

namespace Catalog.API;

public static class CatalogApi
{
    public static IEndpointRouteBuilder MapCatalogApi(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/catalog")
            .WithTags("Catalog API");

        group.MapPost("/movies", async (CreateMovieCommand command, IMediator mediator) =>
        {
            var movieId = await mediator.Send(command);
            return Results.Created($"/api/v1/movies/{movieId}", new { Id = movieId });
        })
        //.RequireAuthorization(PermissionPolicies.Require("catalog.write"))
        .WithName("CreateMovie")
        .WithDescription("Creates a new movie in the catalog.")
        .Produces(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status400BadRequest)
        // Master-data endpoint remains callable directly for Postman setup,
        // but is not part of the booking-client OpenAPI contract.
        .ExcludeFromDescription();

        group.MapPut("/{id:int}", async (int id, UpdateMovieCommand command, IMediator mediator) =>
        {
            if (id != command.Id)
            {
                return Results.BadRequest(new { Error = "ID in URL does not match ID in request body." });
            }

            var isUpdated = await mediator.Send(command);
            return isUpdated ? Results.NoContent() : Results.NotFound(new { Error = "Movie not found." });
        })
        //.RequireAuthorization(PermissionPolicies.Require("catalog.write"))
        .WithName("UpdateMovie")
        .WithDescription("Updates an existing movie in the catalog.")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound)
        .ExcludeFromDescription();

        // Don't MapGet or MapDelete in Comamnd
        group.MapDelete("/{id:int}", async (int id, IMediator mediator) =>
        {
            var command = new DeleteMovieCommand(id);
            var isDeleted = await mediator.Send(command);

            return isDeleted ? Results.NoContent() : Results.NotFound(new { Error = "Movie not found." });
        })
        //.RequireAuthorization(PermissionPolicies.Require("catalog.write"))
        .WithName("DeleteMovie")
        .WithDescription("Deletes a movie from the catalog.")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound)
        .ExcludeFromDescription();

        group.MapGet("/movies", async (IMediator mediator) =>
        {
            var command = new GetMoviesQuery();
            var result = await mediator.Send(command);
            return Results.Ok(result);
        })
        //.RequireAuthorization(PermissionPolicies.Require("catalog.read"))
        .WithName("GetMovies")
        .WithDescription("Retrieves a paginated list of movies from the catalog.")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest);

        group.MapGet("/showtimes", async (
            int? movieId,
            int? cinemaId,
            DateOnly? date,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(
                new GetShowtimesQuery(movieId, cinemaId, date),
                cancellationToken);

            return Results.Ok(result);
        })
        // Authentication/authorization remains disabled during the client-development phase.
        // .RequireAuthorization(PermissionPolicies.Require("catalog.read"))
        .WithName("GetShowtimes")
        .WithDescription("Gets showtimes, optionally filtered by movie, cinema, and date.")
        .Produces<IReadOnlyCollection<ShowtimeDto>>(StatusCodes.Status200OK);

        group.MapPost("/showtimes", async (IMediator mediator, CreateShowtimeCommand command) =>
        {
            var showtimeId = await mediator.Send(command);
            // Trả về mã 201 Created cùng ID của lịch chiếu mới
            return Results.Created($"/api/v1/showtimes/{showtimeId}", new { Id = showtimeId });
        })
        //.RequireAuthorization(PermissionPolicies.Require("catalog.write"))
        .WithName("Create Showtime")
        .WithDescription("Creates a showtime in the catalog.")
        .Produces(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status400BadRequest)
        // Kept for direct Postman use until a web-admin exists.
        .ExcludeFromDescription();

        return endpoints;
    }

}
