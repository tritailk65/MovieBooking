namespace Orchestration.Tests.NUnit;

using MassTransit;
using Microsoft.Extensions.DependencyInjection;

public static class BookingSagaOrchestrationConfigurationExtension
{
    public static IServiceCollection ConfigureMassTransit(
        this IServiceCollection services,
        Action<IBusRegistrationConfigurator>? configure = null)
    {
        services
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
