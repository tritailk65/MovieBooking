using Catalog.API.Application.Showtimes.Commands.CreateShowtime;
using Catalog.API.Domain.Entities;
using Catalog.API.IntegrationEvents;
// Old service-local contract:
// using Catalog.API.IntegrationEvents.Event;
using SagaOrchestration.Contracts;
using Catalog.API.UnitTests.TestSupport;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Microsoft.EntityFrameworkCore;

namespace Catalog.API.UnitTests.Application.Showtimes;

public class CreateShowtimeCommandHandlerTests
{
    /// <summary>
    /// CreateShowtimeCommandHandler -> tạo showtime + publish event
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task Handle_WhenCommandIsValid_ShouldCreateShowtimeAndPublishEvent()
    {
        await using var context = CatalogContextFactory.Create();

        context.Seats.AddRange(
            new Seat("A", 1) { HallId = 10 },
            new Seat("A", 2) { HallId = 10 },
            new Seat("B", 1) { HallId = 20 }
        );
        await context.SaveChangesAsync();

        var eventService = Substitute.For<ICatalogIntegrationEventService>();
        var logger = Substitute.For<ILogger<CreateShowtimeCommandHandler>>();

        var handler = new CreateShowtimeCommandHandler(context, eventService, logger);

        var command = new CreateShowtimeCommand(
            MovieId: 1,
            CinemaId: 2,
            HallId: 10,
            StartTime: new DateTime(2026, 1, 1, 18, 0, 0),
            EndTime: new DateTime(2026, 1, 1, 20, 0, 0),
            BasePrice: 90000m
        );

        var showtimeId = await handler.Handle(command, CancellationToken.None);

        Assert.True(showtimeId > 0);

        await eventService.Received(1)
            .SaveEventAndCatalogContextChangesAsync(
                Arg.Is<ShowtimeCreatedIntegrationEvent>(e =>
                    e.MovieId == 1 &&
                    e.HallId == 10 &&
                    e.BasePrice == 90000m &&
                    e.Seats.Contains("A1") &&
                    e.Seats.Contains("A2") &&
                    !e.Seats.Contains("B1")
                ));

        await eventService.Received(1)
            .PublishThroughEventBusAsync(
                Arg.Is<ShowtimeCreatedIntegrationEvent>(e =>
                    e.Seats.Contains("A1") &&
                    e.Seats.Contains("A2") &&
                    !e.Seats.Contains("B1")
                ));
    }
}
