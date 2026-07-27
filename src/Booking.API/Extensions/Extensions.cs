using BookingService.API.Application.IntegrationEvents.EventHandling;

namespace BookingService.API.Extensions;

public static class Extensions
{
    public static void AddApplicationServices(this IHostApplicationBuilder builder)
    {
        var services = builder.Services;

        services.AddDbContext<BookingContext>(options =>
        {
            options.UseNpgsql(builder.Configuration.GetConnectionString("bookingDb"));
        });

        builder.EnrichNpgsqlDbContext<BookingContext>();

        services.AddMigration<BookingContext, BookingContextSeed>();    

                // Add the integration service that consume the DbContext
        services.AddTransient<IIntegrationEventLogService, IntegrationEventLogService<BookingContext>>();
        services.AddTransient<IBookingIntegrationEventService, BookingIntegrationEventService>();

        builder.AddRabbitMqEventBus("eventbus")
               .AddEventBusSubscriptions();

        services.AddGrpcClient<SeatGrpc.SeatGrpcClient>(options =>
        {
            var seatUrl = builder.Configuration["Grpc:SeatUrl"] ?? "https+http://seat-api";
            options.Address = new Uri(seatUrl);
        });


        // Get authenticate context from middleware
        services.AddHttpContextAccessor();
        services.AddTransient<IIdentityService, IdentityService>();
        
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblyContaining(typeof(Program));

            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(ValidatorBehavior<,>));
            cfg.AddOpenBehavior(typeof(TransactionBehavior<,>));
        });

        services.AddValidatorsFromAssemblyContaining<CreateBookingCommand>();

        // services.AddTransient<IValidator<CreateBookingCommand>,CreateBookingValidatorCommand>();
        // services.AddTransient<IValidator<IdentifiedCommand<CreateBookingCommand, bool>>, IdentifiedCommandValidator>();

        services.AddScoped<IBookingRepository, BookingRepository>();
        services.AddScoped<IBuyerRepository, BuyerRepository>();
        services.AddScoped<IRequestManager, RequestManager>();
        services.AddScoped<IBookingQueries, BookingQueries>();
    }

    // Đăng ký lắng nghe sự kiện để xử lý nội bộ 
    private static void AddEventBusSubscriptions(this IEventBusBuilder eventBus)
    {
        eventBus.AddSubscription<BookingPaymentSucceededIntegrationEvent, BookingPaymentSucceedIntegrationEventHandler>();
        eventBus.AddSubscription<BookingPaymentFailedIntegrationEvent, BookingPaymentFailedIntegrationEventHandler>();

    }


}
