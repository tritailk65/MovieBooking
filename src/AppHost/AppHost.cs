using AppHost;

var builder = DistributedApplication.CreateBuilder(args);

// builder.AddForwardedHeaders();
// Forwarded headers are now configured explicitly by Gateway.API. Enabling the
// environment switch globally made every service trust headers from any source.

var postgres = builder.AddPostgres("postgres")
    .WithImage("ankane/pgvector")
    .WithImageTag("latest")
    .WithPgAdmin(pgadmin => pgadmin
        .WithHttpEndpoint(port: 5050, targetPort: 80, name: "pgadmin", isProxied: false)
        // .WithLifetime(ContainerLifetime.Persistent)
    );
    // .WithLifetime(ContainerLifetime.Persistent);

var catalogDb = postgres.AddDatabase("catalogdb");
var bookingDb = postgres.AddDatabase("bookingdb");
var sagaDb = postgres.AddDatabase("sagadb");

var cache = builder.AddRedis("redis")
            .WithRedisInsight(insight => insight
                    .WithHttpsEndpoint(port: 5051, targetPort: 5540, name: "RedisInsight", isProxied :false)
                    // .WithLifetime(ContainerLifetime.Persistent)
            );
        // .WithLifetime(ContainerLifetime.Persistent);


var rabbitMq = builder.AddRabbitMQ("eventbus");
    // .WithManagementPlugin()  // UI for test
    // .WithLifetime(ContainerLifetime.Persistent);



var catalogApi = builder.AddProject<Projects.Catalog_API>("catalog-api")
    .WithEnvironment("ASPNETCORE_FORWARDEDHEADERS_ENABLED", "false")
    .WithReference(rabbitMq).WaitFor(rabbitMq)
    .WithReference(cache)
    .WithReference(catalogDb)
    // .WithHttpHealthCheck("/health");
    .WithHttpHealthCheck("/health/ready");


var SeatApi = builder.AddProject<Projects.Seat_API>("seat-api")
    .WithEnvironment("ASPNETCORE_FORWARDEDHEADERS_ENABLED", "false")
    .WithReference(cache)
    .WithReference(rabbitMq).WaitFor(rabbitMq)
    .WithHttpHealthCheck("/health/ready");


cache.WithParentRelationship(SeatApi);

var bookingApi = builder.AddProject<Projects.Booking_API>("booking-api")
    .WithEnvironment("ASPNETCORE_FORWARDEDHEADERS_ENABLED", "false")
    .WithReference(rabbitMq).WaitFor(rabbitMq)
    .WithReference(bookingDb).WaitFor(bookingDb)
    .WithReference(sagaDb).WaitFor(sagaDb)
    .WithReference(SeatApi)
    .WaitFor(SeatApi)
    // .WithHttpHealthCheck("/health");
    .WithHttpHealthCheck("/health/ready");

var paymentApi = builder.AddProject<Projects.Payment_API>("payment-api")
    .WithEnvironment("ASPNETCORE_FORWARDEDHEADERS_ENABLED", "false")
    .WithReference(rabbitMq).WaitFor(rabbitMq)
    // .WithHttpHealthCheck("/health");
    .WithHttpHealthCheck("/health/ready");

var gatewayApi = builder.AddProject<Projects.Gateway_API>("gateway-api")
    .WithEnvironment("ASPNETCORE_FORWARDEDHEADERS_ENABLED", "false")
    .WithReference(catalogApi).WaitFor(catalogApi)
    .WithReference(SeatApi).WaitFor(SeatApi)
    .WithReference(bookingApi).WaitFor(bookingApi)
    .WithHttpHealthCheck("/health/ready")
    .WithExternalHttpEndpoints();

builder.Build().Run();
