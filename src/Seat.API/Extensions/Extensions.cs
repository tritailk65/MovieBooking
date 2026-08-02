using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Seat.API.IntegrationEvents.Consumer;

namespace Seat.API.Extensions;

public static class Extensions
{
    public static void AddApplicationServices(this IHostApplicationBuilder builder)
    {
        builder.AddRedisClient("redis");

        //builder.AddDefaultAuthentication();

        //builder.Services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
        //builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();

        builder.Services.AddSingleton<ISeatRepository, SeatRedisRepository>();
        builder.Services.AddSingleton<IRedisLockService, RedisLockService>();
 
        // builder.AddRabbitMqEventBus("eventbus")
        //     .AddSubscription<ShowtimeCreatedIntegrationEvent, ShowtimeCreatedIntegrationEventHandler>()
        //     .AddSubscription<BookingStartedIntegrationEvent, BookingStartedIntegrationEventhandler>()
        //     .AddSubscription<BookingPaymentSucceededIntegrationEvent, BookingPaymentSucceededIntegrationEventHandler>()
        //     .AddSubscription<BookingPaymentFailedIntegrationEvent, BookingPaymentFailedIntegrationEventHandler>()
        //     .ConfigureJsonOptions(options => options.TypeInfoResolverChain.Add(IntegrationEventsContext.Default));

        builder.Services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblyContaining(typeof(SeatEndpoints));
        });

        var rabbitMq = builder.Configuration.GetConnectionString("eventbus")?? throw new InvalidOperationException("Missing ConnectionStrings:eventbus");

        builder.Services.AddMassTransit(x =>
        {
            x.SetEndpointNameFormatter(
                new KebabCaseEndpointNameFormatter("seat", false));

            x.AddConsumer<ConfirmSeatReservationCommandConsumer>();
            x.AddConsumer<ExtendSeatHoldCommandConsumer>();
            x.AddConsumer<ReserveSeatsCommandConsumer>();

            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(new Uri(rabbitMq));

                cfg.UseMessageRetry(retry =>
                {
                    retry.Intervals(
                        TimeSpan.FromSeconds(1),
                        TimeSpan.FromSeconds(5),
                        TimeSpan.FromSeconds(15));
                });

                cfg.ConfigureEndpoints(context);
            });
        });

    }
}

[JsonSerializable(typeof(ShowtimeCreatedIntegrationEvent))]
partial class IntegrationEventsContext : JsonSerializerContext
{

}
