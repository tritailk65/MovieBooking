using AppHost;

var builder = DistributedApplication.CreateBuilder(args);

// builder.AddForwardedHeaders();
// Forwarded headers are now configured explicitly by Gateway.API. Enabling the
// environment switch globally made every service trust headers from any source.

var postgres = builder.AddPostgres("postgres")
    .WithImage("ankane/pgvector")
    .WithImageTag("latest")
    // .WithPgAdmin(pgadmin => pgadmin
    //     .WithHttpEndpoint(port: 5050, targetPort: 80, name: "pgadmin", isProxied: false)
    //     // .WithLifetime(ContainerLifetime.Persistent)
    // );
    .WithLifetime(ContainerLifetime.Persistent);

var catalogDb = postgres.AddDatabase("catalogdb");
var bookingDb = postgres.AddDatabase("bookingdb");
var sagaDb = postgres.AddDatabase("sagadb");

var cache = builder.AddRedis("redis")
            .WithRedisInsight(insight => insight
                    .WithHttpsEndpoint(port: 5051, targetPort: 5540, name: "RedisInsight", isProxied :false)
                    // .WithLifetime(ContainerLifetime.Persistent)
            )
         .WithLifetime(ContainerLifetime.Persistent);

var rabbitMq = builder.AddRabbitMQ("eventbus")
    // .WithManagementPlugin()  // UI for test
    .WithLifetime(ContainerLifetime.Persistent);

#region Config log, trace, metric

var repoRoot = Path.GetFullPath(
    Path.Combine(builder.AppHostDirectory, "..", ".."));

var collectorConfig = Path.Combine(
    repoRoot, "collector-config.yaml");

// Jaeger
var jaeger = builder.AddContainer("jaeger", "jaegertracing/jaeger", "2.20.0")
    .WithHttpEndpoint(port: 16686, targetPort: 16686, name: "ui",isProxied: false)
    .WithEndpoint( targetPort: 4317,name: "otlp-grpc", scheme: "http");

// Elasticsearch
var elasticsearch = builder.AddContainer("elasticsearch","docker.elastic.co/elasticsearch/elasticsearch","9.4.0")
    .WithEnvironment("discovery.type", "single-node")
    .WithEnvironment("xpack.security.enabled", "false")
    .WithEnvironment("xpack.license.self_generated.type", "basic")
    .WithEnvironment("ES_JAVA_OPTS", "-Xms512m -Xmx512m")
    .WithHttpEndpoint(port: 9200, targetPort: 9200, name: "http", isProxied: false)
    .WithVolume("elasticsearch-data", "/usr/share/elasticsearch/data")
    .WithHttpHealthCheck("/_cluster/health")
    .WithLifetime(ContainerLifetime.Persistent);

// Kibana
var kibana = builder.AddContainer("kibana", "docker.elastic.co/kibana/kibana","9.4.0")
    .WithEnvironment( "ELASTICSEARCH_HOSTS", elasticsearch.GetEndpoint("http"))
    .WithEnvironment("XPACK_SECURITY_ENABLED", "false")
    .WithEnvironment("SERVER_HOST", "0.0.0.0")
    .WithHttpEndpoint(port: 5601,targetPort: 5601,name: "http",isProxied: false)
    .WaitFor(elasticsearch);

// OTel Collector
var otelCollector = builder.AddContainer("otel-collector", "docker.elastic.co/elastic-agent/elastic-agent","9.4.0")
    .WithArgs("--config=/etc/otelcol/collector-config.yaml")
    .WithEnvironment("ELASTIC_AGENT_OTEL", "true")
    .WithBindMount( collectorConfig, "/etc/otelcol/collector-config.yaml", isReadOnly: true)
    .WithEndpoint(port: 4317,targetPort: 4317,name: "otlp-grpc",scheme: "http",isProxied: false)
    .WithHttpEndpoint( port: 4318,targetPort: 4318, name: "otlp-http",isProxied: false)
    .WithHttpEndpoint(port: 13133,targetPort: 13133, name: "health", isProxied: false)
    .WaitFor(elasticsearch)
    .WaitForStart(jaeger);

otelCollector.WithHttpHealthCheck(
    () => otelCollector.GetEndpoint("health"),
    path: "/");
#endregion

var catalogApi = builder.AddProject<Projects.Catalog_API>("catalog-api")
    .WithEnvironment("ASPNETCORE_FORWARDEDHEADERS_ENABLED", "false")
    .WithEnvironment(
        "OTEL_EXPORTER_OTLP_ENDPOINT",
        otelCollector.GetEndpoint("otlp-grpc"))
    .WithEnvironment("OTEL_EXPORTER_OTLP_PROTOCOL", "grpc")
    .WaitFor(otelCollector)
    .WithReference(rabbitMq).WaitFor(rabbitMq)
    .WithReference(cache)
    .WithReference(catalogDb)
    .WithHttpHealthCheck("/health/ready");


var SeatApi = builder.AddProject<Projects.Seat_API>("seat-api")
    .WithEnvironment("ASPNETCORE_FORWARDEDHEADERS_ENABLED", "false")
        .WithEnvironment(
        "OTEL_EXPORTER_OTLP_ENDPOINT",
        otelCollector.GetEndpoint("otlp-grpc"))
    .WithEnvironment("OTEL_EXPORTER_OTLP_PROTOCOL", "grpc")
    .WaitFor(otelCollector)
    .WithReference(cache)
    .WithReference(rabbitMq).WaitFor(rabbitMq)
    .WithHttpHealthCheck("/health/ready");


cache.WithParentRelationship(SeatApi);

var bookingApi = builder.AddProject<Projects.Booking_API>("booking-api")
    .WithEnvironment("ASPNETCORE_FORWARDEDHEADERS_ENABLED", "false")
        .WithEnvironment(
        "OTEL_EXPORTER_OTLP_ENDPOINT",
        otelCollector.GetEndpoint("otlp-grpc"))
    .WithEnvironment("OTEL_EXPORTER_OTLP_PROTOCOL", "grpc")
    .WaitFor(otelCollector)
    .WithReference(rabbitMq).WaitFor(rabbitMq)
    .WithReference(bookingDb).WaitFor(bookingDb)
    .WithReference(sagaDb).WaitFor(sagaDb)
    .WithEnvironment(
        "Grpc__SeatUrl",
        SeatApi.GetEndpoint("https"))
    .WithReference(SeatApi)
    .WaitFor(SeatApi)
    // .WithHttpHealthCheck("/health");
    .WithHttpHealthCheck("/health/ready");

var paymentApi = builder.AddProject<Projects.Payment_API>("payment-api")
    .WithEnvironment("ASPNETCORE_FORWARDEDHEADERS_ENABLED", "false")
        .WithEnvironment(
        "OTEL_EXPORTER_OTLP_ENDPOINT",
        otelCollector.GetEndpoint("otlp-grpc"))
    .WithEnvironment("OTEL_EXPORTER_OTLP_PROTOCOL", "grpc")
    .WaitFor(otelCollector)
    .WithReference(rabbitMq).WaitFor(rabbitMq)
    // .WithHttpHealthCheck("/health");
    .WithHttpHealthCheck("/health/ready");

var gatewayApi = builder.AddProject<Projects.Gateway_API>("gateway-api")
    .WithEnvironment("ASPNETCORE_FORWARDEDHEADERS_ENABLED", "false")
        .WithEnvironment(
        "OTEL_EXPORTER_OTLP_ENDPOINT",
        otelCollector.GetEndpoint("otlp-grpc"))
    .WithEnvironment("OTEL_EXPORTER_OTLP_PROTOCOL", "grpc")
    .WaitFor(otelCollector)
    .WithReference(catalogApi).WaitFor(catalogApi)
    .WithReference(SeatApi).WaitFor(SeatApi)
    .WithReference(bookingApi).WaitFor(bookingApi)
    .WithHttpHealthCheck("/health/ready")
    .WithExternalHttpEndpoints();

builder.Build().Run();
