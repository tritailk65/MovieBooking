using Microsoft.AspNetCore.Authorization;
using ServiceDefaults.Authorization;

namespace Seat.API.Extensions;

public static class Extensions
{
    public static void AddApplicationServices(this IHostApplicationBuilder builder)
    {
        builder.AddRedisClient("redis");

        builder.AddDefaultAuthentication();

        builder.Services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
        builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();

        builder.Services.AddSingleton<ISeatRepository, SeatRedisRepository>();
        builder.Services.AddSingleton<IRedisLockService, RedisLockService>();
 
        builder.AddRabbitMqEventBus("eventbus")
            .AddSubscription<ShowtimeCreatedIntegrationEvent, ShowtimeCreatedIntegrationEventHandler>()
            .AddSubscription<BookingStartedIntegrationEvent, BookingStartedIntegrationEventhandler>()
            .AddSubscription<BookingPaymentSucceededIntegrationEvent, BookingPaymentSucceededIntegrationEventHandler>()
            .AddSubscription<BookingPaymentFailedIntegrationEvent, BookingPaymentFailedIntegrationEventHandler>()
            .ConfigureJsonOptions(options => options.TypeInfoResolverChain.Add(IntegrationEventsContext.Default));

        builder.Services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblyContaining(typeof(SeatEndpoints));
        });

    }
}

[JsonSerializable(typeof(ShowtimeCreatedIntegrationEvent))]
partial class IntegrationEventsContext : JsonSerializerContext
{

}
