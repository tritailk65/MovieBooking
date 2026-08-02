namespace Booking.Saga.IntegrationTests;

using BookingService.API;
using BookingService.API.Application.Commands.CreateBooking;
using BookingService.API.Infrastructure;
using BookingService.Infrastructure;
using Catalog.API;
using Catalog.API.Application.Showtimes.Commands.CreateShowtime;
using Catalog.API.Infrastucture;
using Catalog.API.IntegrationEvents.Event;
using MassTransit;
using MassTransit.Testing;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SagaOrchestration;
using Seat.API.Application.Command.LockSeat;
using Seat.API.Domain.Interfaces;
using Seat.API.Infrastructure.Redis;
using Seat.API.IntegrationEvents.EventHandlers;
using Shared.Infrastructure.OrderSaga;
using StackExchange.Redis;

[TestFixture]
public class BookingSagaE2ETest
{
    private ServiceProvider _provider;
    private ITestHarness _harness;
    private ISagaStateMachineTestHarness<BookingStateMachine, BookingSaga> _sagaHarness = null!;
    private IMediator _mediator;
    #region Setup and TearDown

    [SetUp]
    public async Task SetUp()
    {
        var redisTestConnectionString = Environment.GetEnvironmentVariable("REDIS_TEST_CONNECTION_STRING") ?? 
                                                        "localhost:16379,abortConnect=false,connectTimeout=5000";

        var builder = WebApplication.CreateBuilder(
            new WebApplicationOptions
            {
                EnvironmentName = "IntegrationTest",
                // ContentRootPath =  Path.GetFullPath(
                //         Path.Combine(TestContext.CurrentContext.TestDirectory,
                //             "../src/Catalog.API"))
            });

        builder.Configuration.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["ConnectionStrings:redis"] = redisTestConnectionString
            });

        builder.AddRedisClient("redis");
        builder.Services.AddSingleton<ISeatRepository, SeatRedisRepository>();
        builder.Services.AddSingleton<IRedisLockService, RedisLockService>();

        builder.Services.ConfigureMassTransit(x =>
        {
            x.AddSagaStateMachine<BookingStateMachine, BookingSaga>();

            x.AddConsumer<ShowtimeCreatedIntegrationEvent, ShowtimeCreatedIntegrationEventHandler>();
             // Add Consumer
        });

        builder.Services.AddScoped<CatalogContextSeed>();
        builder.Services.AddScoped<BookingContextSeed>();

        builder.Services.AddRouting();
        builder.Services.AddMediatR(configuration =>
        {
            configuration.RegisterServicesFromAssemblies(
                typeof(CreateShowtimeCommand).Assembly, // Catalog.API
                typeof(LockSeatCommand).Assembly,       // Seat.API
                typeof(CreateBookingCommand).Assembly   // Booking.API
            );
        });

        _provider = builder.Services.BuildServiceProvider(validateScopes: true);

        await InitializeDatabasesAsync();

        _harness = _provider.GetTestHarness();

        await _harness.Start();

        _sagaHarness = _harness.GetSagaStateMachineHarness<BookingStateMachine, BookingSaga>();
    }

    [TearDown]
    public async Task TearDown()
    {
        await _harness.Stop();
        await _provider.DisposeAsync();
    }

    private async Task InitializeDatabasesAsync()
    {
        await using var scope = _provider.CreateAsyncScope();
        var services = scope.ServiceProvider;

        var sagaDb = services.GetRequiredService<BookingSagaContext>();
        await sagaDb.Database.MigrateAsync();

        var catalogDb = services.GetRequiredService<CatalogContext>();
        await catalogDb.Database.MigrateAsync();

        var catalogSeeder = services.GetRequiredService<CatalogContextSeed>();
        await catalogSeeder.SeedAsync(catalogDb);

        var bookingDb = services.GetRequiredService<BookingContext>();
        await bookingDb.Database.MigrateAsync();

        var bookingSeeder = services.GetRequiredService<BookingContextSeed>();
        await bookingSeeder.SeedAsync(bookingDb);

        // Cleanup redis test
        var redis = _provider.GetRequiredService<IConnectionMultiplexer>();
        await ClearTestRedisAsync(redis);
    }

    private static async Task ClearTestRedisAsync( IConnectionMultiplexer redis)
    {
        var endpoint = redis.GetEndPoints().Single().ToString();

        if (!endpoint.Contains("16379") &&
            !endpoint.Contains("redis-test"))
        {
            throw new InvalidOperationException(
                $"Refusing to clear non-test Redis: {endpoint}");
        }

        await redis.GetDatabase().ExecuteAsync("FLUSHDB");
    }
    #endregion

    #region Mock data
    private const int ShowtimeId = 1;
    private const string UserId = "2779fb04-052e-49c1-8ce0-c200d8e06b6f";
    private const decimal TotalPrice = 180_000m;
    private sealed record SagaTestContext(
        Guid ReservationId,
        int BookingId,
        string PaymentId);
    #endregion

    #region  Helper Function
    public async Task CreateShowTime()
    {


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
    #endregion

    [Test]
    public async Task E2E_Orchestration_HappyPath()
    {
        
    }

}
