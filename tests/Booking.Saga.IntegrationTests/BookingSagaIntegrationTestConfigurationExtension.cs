using BookingService.Infrastructure;
using Catalog.API.Infrastucture;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using SagaOrchestration;
using Seat.API.Domain.Interfaces;
using Seat.API.Infrastructure.Redis;


namespace Booking.Saga.IntegrationTests;


public static class BookingSagaIntegrationTestConfigurationExtension
{
    public static IServiceCollection ConfigureMassTransit(this IServiceCollection services, Action<IBusRegistrationConfigurator> configure = null)
    {
        var runId = Guid.NewGuid().ToString("N");

        var postgresConnection = $"Host=localhost;Username=postgres;Password=123;Port=15432";

        var catalogConnection =
            $"{postgresConnection};Database=catalog_test_{runId}";

        var bookingConnection =
            $"{postgresConnection};Database=booking_test_{runId}";

        var sagaConnection =
            $"{postgresConnection};Database=saga_test_{runId}";

        services
            .AddDbContext<BookingSagaContext>(option =>
            {
                option.UseNpgsql(sagaConnection, x => x.MigrationsAssembly(typeof(BookingSagaContext).Assembly.FullName));
            })
            .AddDbContext<CatalogContext>(option =>
            {
                option.UseNpgsql(catalogConnection, x => x.MigrationsAssembly(typeof(CatalogContext).Assembly.FullName));
            })
            .AddDbContext<BookingContext>(option =>
            {
                option.UseNpgsql(bookingConnection, x => x.MigrationsAssembly(typeof(BookingContext).Assembly.FullName));
            })
            .AddQuartz(x =>
            {
                x.UseMicrosoftDependencyInjectionJobFactory();
            })
            .AddMassTransitTestHarness(x =>
            {
                x.SetKebabCaseEndpointNameFormatter();

                x.AddPublishMessageScheduler();

                configure?.Invoke(x);

                x.UsingInMemory((context, cfg) =>
                {
                    cfg.UsePublishMessageScheduler();

                    cfg.ConfigureEndpoints(context);
                });
            });

        return services;
    }
}

public class CatalogDbContextFactory : ApplicationDbContextFactory<CatalogContext>
{
}

public class BookingDbContextFactory : ApplicationDbContextFactory<BookingContext>
{
}

public static class SeatRedisExtensions
{
    public static IServiceCollection AddSeatRedisServices(
        this IServiceCollection services)
    {
        services.AddSingleton<ISeatRepository, SeatRedisRepository>();
        services.AddSingleton<IRedisLockService, RedisLockService>();

        return services;
    }
}