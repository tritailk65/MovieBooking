namespace Booking.Saga.IntegrationTests;

using BookingService.API;
using BookingService.API.Application.Commands.CreateBooking;
using BookingService.API.Application.Commands.SetAwaitingPayment;
using BookingService.API.Application.IntegrationEvents;
using BookingService.API.Application.IntegrationEvents.Consumers;
using BookingService.API.Application.Models;
using BookingService.API.Infrastructure;
using BookingService.API.IntegrationEvents;
using BookingService.Domain.AggregateModel.BookingAggregates;
using BookingService.Domain.AggregateModel.BuyerAggregate;
using BookingService.Infrastructure;
using BookingService.Infrastructure.Repository;
using Catalog.API;
using Catalog.API.Application.Showtimes.Commands.CreateShowtime;
using Catalog.API.Infrastucture;
using Catalog.API.IntegrationEvents;
using IntegrationEventLogEF.Services;
using MassTransit;
using MassTransit.Testing;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PaymentService.Api;
using PaymentService.Api.IntegrationEvents.Consumers;
using SagaOrchestration;
using SagaOrchestration.Contracts;
using Seat.API.Application.Command.LockSeat;
using Seat.API.Domain.Interfaces;
using Seat.API.Endpoints;
using Seat.API.Infrastructure.Redis;
using Seat.API.IntegrationEvents.Consumer;
using Seat.API.IntegrationEvents.EventHandlers;
using StackExchange.Redis;

[TestFixture]
public class BookingSagaE2ETest
{
    private WebApplication _app = null!;
    private IServiceProvider _provider = null!;
    private ITestHarness _harness;
    private ISagaStateMachineTestHarness<BookingStateMachine, BookingSaga> _sagaHarness = null!;
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

        builder.WebHost.UseTestServer();

        builder.Configuration.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["ConnectionStrings:redis"] = redisTestConnectionString
            });

        builder.AddRedisClient("redis");
        builder.Services.AddSingleton<ISeatRepository, SeatRedisRepository>();
        builder.Services.AddSingleton<IRedisLockService, RedisLockService>();
        builder.Services.AddTransient<ShowtimeCreatedIntegrationEventHandler>();
        builder.Services.AddScoped<ICatalogIntegrationEventService, TestCatalogIntegrationEventService>();
        builder.Services.AddScoped<IBookingIntegrationEventService, TestBookingIntegrationEventService>();

        builder.Services.ConfigureMassTransit(x =>
        {
            x.AddSagaStateMachine<BookingStateMachine, BookingSaga>();

            x.AddConsumer<ConfirmSeatReservationCommandConsumer>();
            x.AddConsumer<ExtendSeatHoldCommandConsumer>();
            x.AddConsumer<ReserveSeatsCommandConsumer>();

            x.AddConsumer<MarkBookingPaidCommandConsumer>();
            x.AddConsumer<RequestPaymentCommandConsumer>();
            x.AddConsumer<ShowtimeCreatedIntegrationEventConsumer>();
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

        builder.Services.AddScoped<IBookingRepository, BookingRepository>();
        builder.Services.AddScoped<IBuyerRepository, BuyerRepository>();


        builder.Services.Configure<PaymentOptions>(options =>
        {
            options.PaymentSucceeded = true;
        });

        _app = builder.Build();
        _app.MapCatalogApi();
        _app.MapSeatApi();
        _provider = _app.Services;

        await InitializeDatabasesAsync();

        await _app.StartAsync();

        _harness = _provider.GetTestHarness();

        _sagaHarness = _harness.GetSagaStateMachineHarness<BookingStateMachine, BookingSaga>();
    }

    [TearDown]
    public async Task TearDown()
    {
        await _app.StopAsync();
        await _app.DisposeAsync();
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
        await bookingDb.Database.EnsureCreatedAsync();

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
    private async Task<SagaPrerequisiteResult> PrepareSagaPrerequisitesAsync(
        CancellationToken cancellationToken = default)
    {
        var apiClient = _app.GetTestClient();
        var seatRepository = _provider.GetRequiredService<ISeatRepository>();
        var redisLockService = _provider.GetRequiredService<IRedisLockService>();
        var request = new CreateShowtimeCommand(
            MovieId: 1,
            CinemaId: 2,
            HallId: 3,
            StartTime: DateTime.UtcNow.AddHours(1),
            EndTime: DateTime.UtcNow.AddHours(3),
            BasePrice: 90_000m);

        return await SagaPrerequisiteApiHelper.CreateShowtimeAndLockSeatsAsync(
            apiClient,
            apiClient,
            seatRepository,
            redisLockService,
            request,
            UserId,
            seatCount: 1,
            cancellationToken: cancellationToken);
    }
    #endregion

    [Test]
    public async Task E2E_Orchestration_HappyPath()
    {
        var prerequisite = await PrepareSagaPrerequisitesAsync();

        await using (var scope = _provider.CreateAsyncScope())
        {
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var seatPrice = prerequisite.TotalPrice / prerequisite.SeatIds.Count;
            var bookingItems = prerequisite.SeatIds.Select(seatId => new SeatItem
            {
                ShowtimeId = prerequisite.ShowtimeId,
                SeatId = seatId,
                BasePrice = seatPrice
            });

            var bookingCreated = await mediator.Send(new CreateBookingCommand(
                bookingItems,
                prerequisite.UserId,
                "Saga E2E User",
                prerequisite.ShowtimeId,
                prerequisite.ReservationId));

            Assert.That(bookingCreated, Is.True, "Booking was not created");
        }

        int bookingId;
        await using (var scope = _provider.CreateAsyncScope())
        {
            var bookingContext = scope.ServiceProvider.GetRequiredService<BookingContext>();
            bookingId = await bookingContext.Bookings
                .Where(booking => booking.ReservationId == prerequisite.ReservationId)
                .Select(booking => booking.Id)
                .SingleAsync();
        }

        Assert.That(
            await _sagaHarness.Exists(prerequisite.ReservationId, state => state.PendingPayment),
            Is.EqualTo(prerequisite.ReservationId),
            "Saga did not reach PendingPayment");

        await using (var scope = _provider.CreateAsyncScope())
        {
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var paymentRequested = await mediator.Send(
                new SetAwaitingPaymentBookingStatusCommand(bookingId));

            Assert.That(paymentRequested, Is.True, "Booking could not start payment");
        }

        Assert.That(
            await _sagaHarness.NotExists(prerequisite.ReservationId),
            Is.Null,
            "Saga was not finalized after the booking was paid");

        Assert.That(await _harness.Consumed.Any<ReserveSeatsCommand>(), Is.True);
        Assert.That(await _harness.Consumed.Any<ExtendSeatHoldCommand>(), Is.True);
        Assert.That(await _harness.Consumed.Any<RequestPaymentCommand>(), Is.True);
        Assert.That(await _harness.Consumed.Any<ConfirmSeatReservationCommand>(), Is.True);
        Assert.That(await _harness.Consumed.Any<MarkBookingPaidCommand>(), Is.True);

        await using (var scope = _provider.CreateAsyncScope())
        {
            var bookingContext = scope.ServiceProvider.GetRequiredService<BookingContext>();
            var booking = await bookingContext.Bookings
                .Include(current => current.BookingStatus)
                .SingleAsync(current => current.Id == bookingId);

            Assert.That(booking.BookingStatus.Id, Is.EqualTo(BookingStatus.Paid.Id));
        }
    }

}
