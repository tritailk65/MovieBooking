using MassTransit;
using PaymentService.Api.IntegrationEvents.Consumers;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// builder.AddRabbitMqEventBus("EventBus")
//     .AddSubscription<BookingStatusChangedToAwaitingPaymentIntegrationEvent, BookingStatusChangedToAwaitingPaymentIntegrationEventHandler>();

builder.Services.AddOptions<PaymentOptions>()
    .BindConfiguration(nameof(PaymentOptions));


var rabbitMq = builder.Configuration
    .GetConnectionString("eventbus")
    ?? throw new InvalidOperationException(
        "Missing ConnectionStrings:eventbus");

builder.Services.AddMassTransit(x =>
{
    x.SetEndpointNameFormatter(
        new KebabCaseEndpointNameFormatter("payment", false));

    x.AddConsumer<RequestPaymentCommandConsumer>();

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

var app = builder.Build();

app.MapDefaultEndpoints();

await app.RunAsync();
