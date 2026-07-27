using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace ServiceDefaults
{
    public static partial class Extensions
    {
        public static IHostApplicationBuilder AddServiceDefaults(this IHostApplicationBuilder builder)
        {
            builder.AddBasicServiceDefaults();

            builder.Services.AddServiceDiscovery();

            builder.Services.ConfigureHttpClientDefaults(http =>
            {
                // Http error handler (circuit-breaker, retry... )
                http.AddStandardResilienceHandler();

                // Allow service call service by name
                http.AddServiceDiscovery();
            });

            return builder;
        }

        /// <summary>
        /// Adds the services except for making outgoing HTTP calls.
        /// </summary>
        /// <remarks>
        /// Hàm này bỏ cấu hình http client để dành cho các service không cần polly như background worker, consumer,...
        /// </remarks>
        public static IHostApplicationBuilder AddBasicServiceDefaults(this IHostApplicationBuilder builder)
        {
            // Default health checks assume the event bus and self health checks
            builder.AddDefaultHealthChecks();

            //open telementry
            builder.ConfigureOpenTelemetry();

            return builder;
        }

        public static IHostApplicationBuilder ConfigureOpenTelemetry (this IHostApplicationBuilder builder)
        {

            // OpenTelementry configuration
            builder.Logging.AddOpenTelemetry(logging =>
            {
                logging.IncludeFormattedMessage = true;
                logging.IncludeScopes = true;
            });

            
            builder.Services.AddOpenTelemetry()
                .WithMetrics(metrics =>
                {
                    //  request, ram, cpu,.. infomation
                    metrics.AddAspNetCoreInstrumentation()
                            .AddHttpClientInstrumentation()
                            .AddRuntimeInstrumentation()
                            .AddMeter("Experimental.Microsoft.Extensions.AI");
                })
                .WithTracing(tracing =>
                {
                    // truy vết theo request

                    if (builder.Environment.IsDevelopment())
                    {
                        // We want to view all traces in development
                        tracing.SetSampler(new AlwaysOnSampler());
                    }

                    tracing.AddAspNetCoreInstrumentation()
                        .AddGrpcClientInstrumentation()
                        .AddHttpClientInstrumentation()
                        .AddSource("Experimental.Microsoft.Extensions.AI");
                });

            builder.AddOpenTelemetryExporters();

            return builder;
        }

        private static IHostApplicationBuilder AddOpenTelemetryExporters(this IHostApplicationBuilder builder)
        {
            var useOtlpExporter = !string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);

            // Nếu khai báo địa chỉ đích, ví dụ aspire dashboard, jeager, hay prometheus
            if (useOtlpExporter)
            {
                builder.Services.Configure<OpenTelemetryLoggerOptions>(logging => logging.AddOtlpExporter());   // Giao thức OTLP
                builder.Services.ConfigureOpenTelemetryMeterProvider(metrics => metrics.AddOtlpExporter());
                builder.Services.ConfigureOpenTelemetryTracerProvider(tracing => tracing.AddOtlpExporter());
            }

            return builder;
        }

        
        public static IHostApplicationBuilder AddDefaultHealthChecks(this IHostApplicationBuilder builder)
        {
            builder.Services.AddHealthChecks()
                // Add a default liveness check to ensure app is responsive
                .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

            return builder;
        }

        public static WebApplication MapDefaultEndpoints(this WebApplication app)
        {
            // Uncomment the following line to enable the Prometheus endpoint (requires the OpenTelemetry.Exporter.Prometheus.AspNetCore package)
            // app.MapPrometheusScrapingEndpoint();

            // Adding health checks endpoints to applications in non-development environments has security implications.
            // See https://aka.ms/dotnet/aspire/healthchecks for details before enabling these endpoints in non-development environments.
            if (app.Environment.IsDevelopment())    
            {
                // All health checks must pass for app to be considered ready to accept traffic after starting
                app.MapHealthChecks("/health");

                // Only health checks tagged with the "live" tag must pass for app to be considered alive
                app.MapHealthChecks("/alive", new HealthCheckOptions
                {
                    Predicate = r => r.Tags.Contains("live")    // Chỉ lấy các ứng dụng live
                });
            }

            return app;
        }
    }
}
