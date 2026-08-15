using IntegrationEventLogEF.Services;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using ServiceDefaults.Authorization;

namespace Catalog.API.Extensions
{
    public static class Extension
    {
        public static void AddApplicationServices(this IHostApplicationBuilder builder)
        {
            //builder.AddDefaultAuthentication();

            //builder.Services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
            //builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
            
            // Avoid loading full database config and migrations if startup
            // is being invoked from build-time OpenAPI generation
            
            if (builder.Environment.IsBuild())
            {
                builder.Services.AddDbContext<CatalogContext>();
                return;
            }

            builder.AddNpgsqlDbContext<CatalogContext>("catalogdb");

            builder.Services.AddMigration<CatalogContext, CatalogContextSeed>();

            builder.Services.AddOptions<CatalogOptions>().BindConfiguration(nameof(CatalogOptions));

            // Configure mediator
            builder.Services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssemblyContaining(typeof(CatalogApi));
            });


            builder.Services.AddValidatorsFromAssemblyContaining<CreateMovieCommandValidator>();
            builder.Services.AddValidatorsFromAssemblyContaining<UpdateMovieCommandValitor>();
            builder.Services.AddValidatorsFromAssemblyContaining<DeleteMovieCommandValidator>();

            // redis cache
            builder.AddRedisDistributedCache("redis");

            // Outbox pattern
            builder.Services.AddTransient<IIntegrationEventLogService, IntegrationEventLogService<CatalogContext>>();

            // Rabbit mq
            builder.Services.AddTransient<ICatalogIntegrationEventService, CatalogIntegrationEventService>();
            // Old custom event bus used a different RabbitMQ exchange/envelope than
            // the MassTransit consumers in Seat.API.
            // builder.AddRabbitMqEventBus("eventbus");

            var rabbitMq = builder.Configuration.GetConnectionString("eventbus")
                ?? throw new InvalidOperationException("Missing ConnectionStrings:eventbus");

            builder.Services.AddMassTransit(x =>
            {
                x.SetEndpointNameFormatter(
                    new KebabCaseEndpointNameFormatter("catalog", false));

                x.UsingRabbitMq((context, cfg) =>
                {
                    cfg.Host(new Uri(rabbitMq));
                    cfg.ConfigureEndpoints(context);
                });
            });
            
            //option
            builder.Services.AddOptions<CatalogOptions>().BindConfiguration(nameof(CatalogOptions));
        }
    }
}
