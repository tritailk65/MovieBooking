var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddRabbitMqEventBus("EventBus")
    .AddSubscription<BookingStatusChangedToAwaitingPaymentIntegrationEvent, BookingStatusChangedToAwaitingPaymentIntegrationEventHandler>();

builder.Services.AddOptions<PaymentOptions>()
    .BindConfiguration(nameof(PaymentOptions));

var app = builder.Build();

app.MapDefaultEndpoints();

await app.RunAsync();
