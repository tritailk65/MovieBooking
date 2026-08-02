using BookingService.API.Application.IntegrationEvents.Consumers;
using BookingService.API.Application.IntegrationEvents.EventHandling;
using MassTransit;
using Quartz;
using SagaOrchestration;

namespace BookingService.API.Extensions;

public static class Extensions
{
    public static void AddApplicationServices(this IHostApplicationBuilder builder)
    {
        var services = builder.Services;

        //builder.AddDefaultAuthentication();

        //services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
        //services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
            
        services.AddDbContext<BookingContext>(options =>
        {
            options.UseNpgsql(builder.Configuration.GetConnectionString("bookingDb"));
        });

        builder.EnrichNpgsqlDbContext<BookingContext>();

        services.AddMigration<BookingContext, BookingContextSeed>();    

        // Add the integration service that consume the DbContext
        services.AddTransient<IIntegrationEventLogService, IntegrationEventLogService<BookingContext>>();
        services.AddTransient<IBookingIntegrationEventService, BookingIntegrationEventService>();

        // builder.AddRabbitMqEventBus("eventbus")
        //        .AddEventBusSubscriptions();

        var rabbitMq = builder.Configuration.GetConnectionString("eventbus") ?? throw new InvalidOperationException("Missing ConnectionStrings:eventbus");

        var sagaConnection = builder.Configuration.GetConnectionString("sagadb") ?? throw new InvalidOperationException("Missing ConnectionStrings:sagadb");

        services.AddDbContext<BookingSagaContext>(options =>
        {
            options.UseNpgsql(sagaConnection, postgres => postgres.MigrationsAssembly(typeof(BookingSagaContext).Assembly.FullName));
        });

        services.AddQuartz();

        services.AddQuartzHostedService(options =>
        {
            options.WaitForJobsToComplete = true;
        });

        services.AddMassTransit(x =>
        {
            x.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter("booking", false));

            x.AddQuartzConsumers();
            x.AddPublishMessageScheduler();

            x.AddEntityFrameworkOutbox<BookingSagaContext>(outbox =>
            {
                outbox.UsePostgres();
                outbox.UseBusOutbox();
            });

            x.AddSagaStateMachine<BookingStateMachine,BookingSaga,BookingSagaDefinition>()
                .EntityFrameworkRepository(repository =>
                {
                    repository.ConcurrencyMode =
                        ConcurrencyMode.Pessimistic;

                    repository.ExistingDbContext<BookingSagaContext>();
                    repository.UsePostgres();
                });

            x.AddConsumer<MarkBookingPaidCommandConsumer>();


            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(new Uri(rabbitMq));

                cfg.UsePublishMessageScheduler();

                cfg.ConfigureEndpoints(context);
            });
        });

        // services.AddOptions<SeatServiceAuthenticationOptions>()
        //     .BindConfiguration(SeatServiceAuthenticationOptions.SectionName)
        //     .ValidateDataAnnotations()
        //     .ValidateOnStart();

        // services.AddHttpClient<SeatServiceTokenProvider>();
        // services.AddTransient<SeatServiceAuthorizationHandler>();

        services.AddGrpcClient<SeatGrpc.SeatGrpcClient>(options =>
            {
                var seatUrl = builder.Configuration["Grpc:SeatUrl"] ?? "http://seat-api";
                options.Address = new Uri(seatUrl);
            });
            // .AddServiceDiscovery();
            //.AddHttpMessageHandler<SeatServiceAuthorizationHandler>();


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
