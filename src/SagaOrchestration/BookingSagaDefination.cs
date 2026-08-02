namespace SagaOrchestration;

public sealed class BookingSagaDefinition : SagaDefinition<BookingSaga>
{
    protected override void ConfigureSaga(
        IReceiveEndpointConfigurator endpoint,
        ISagaConfigurator<BookingSaga> saga,
        IRegistrationContext context)
    {
        endpoint.UseMessageRetry(retry =>
        {
            retry.Intervals(
                TimeSpan.FromMilliseconds(500),
                TimeSpan.FromSeconds(2),
                TimeSpan.FromSeconds(5));
        });

        endpoint.UseEntityFrameworkOutbox<BookingSagaContext>(
            context);
    }
}