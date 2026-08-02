using AppHost;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddForwardedHeaders();

var postgres = builder.AddPostgres("postgres")
    .WithImage("ankane/pgvector")
    .WithImageTag("latest")
    .WithPgAdmin(pgadmin => pgadmin
        .WithHttpEndpoint(port: 5050, targetPort: 80, name: "pgadmin", isProxied: false)
        .WithLifetime(ContainerLifetime.Persistent)
    )
    .WithLifetime(ContainerLifetime.Persistent);

var catalogDb = postgres.AddDatabase("catalogdb");
var bookingDb = postgres.AddDatabase("bookingdb");
var sagaDb = postgres.AddDatabase("sagadb");

var cache = builder.AddRedis("redis")
            .WithRedisInsight(insight => insight
                    .WithHttpsEndpoint(port: 5051, targetPort: 5540, name: "RedisInsight", isProxied :false)
                    .WithLifetime(ContainerLifetime.Persistent)
            )
        .WithLifetime(ContainerLifetime.Persistent);


var rabbitMq = builder.AddRabbitMQ("eventbus")
    // .WithManagementPlugin()  // UI for test
    .WithLifetime(ContainerLifetime.Persistent);



var catalogApi = builder.AddProject<Projects.Catalog_API>("catalog-api")
    .WithReference(rabbitMq).WaitFor(rabbitMq)
    .WithReference(cache)
    .WithReference(catalogDb)
    .WithHttpHealthCheck("/health");


var SeatApi = builder.AddProject<Projects.Seat_API>("seat-api")
    .WithReference(cache)
    .WithReference(rabbitMq).WaitFor(rabbitMq);


cache.WithParentRelationship(SeatApi);

var bookingApi = builder.AddProject<Projects.Booking_API>("booking-api")
    .WithReference(rabbitMq).WaitFor(rabbitMq)
    .WithReference(bookingDb).WaitFor(bookingDb)
    .WithReference(sagaDb).WaitFor(sagaDb)
    .WithReference(SeatApi)
    .WaitFor(SeatApi)
    .WithHttpHealthCheck("/health");

var paymentApi = builder.AddProject<Projects.Payment_API>("payment-api")
    .WithReference(rabbitMq).WaitFor(rabbitMq)
    .WithHttpHealthCheck("/health");

builder.Build().Run();
