using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.StaticFiles.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace ServiceDefaults
{
    public static partial class Extensions
    {
        public static IHostApplicationBuilder AddServiceDefaults(this IHostApplicationBuilder builder, string serviceName)
        {
            builder.AddBasicServiceDefaults(serviceName);

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
        public static IHostApplicationBuilder AddBasicServiceDefaults(this IHostApplicationBuilder builder, string serviceName)
        {
            // Default health checks assume the event bus and self health checks
            builder.AddDefaultHealthChecks();

            //open telementry
            builder.ConfigureOpenTelemetry(serviceName);

            return builder;
        }

        public static IHostApplicationBuilder ConfigureOpenTelemetry (this IHostApplicationBuilder builder, string serviceName)
        {

            // OpenTelementry configuration
            builder.Logging.AddOpenTelemetry(logging =>
            {
                logging.IncludeFormattedMessage = true;
                logging.IncludeScopes = true;
                logging.ParseStateValues = true;
            });

            
            builder.Services.AddOpenTelemetry()
                .ConfigureResource(resource => resource
                .AddService(
                    serviceName: serviceName,
                    serviceNamespace: "OpenTelemetryLab",
                    serviceVersion: "1.0.0",
                    serviceInstanceId: Environment.MachineName)
                .AddAttributes(
                [
                    new KeyValuePair<string, object>("deployment.environment.name", builder.Environment.EnvironmentName)
                ]))
                .WithMetrics(metrics =>
                {
                    //  request, ram, cpu,.. infomation
                    metrics.AddAspNetCoreInstrumentation()
                            .AddHttpClientInstrumentation()
                            .AddRuntimeInstrumentation()
                            .AddMeter(ActivityExtensions.MeterName)
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
                        .AddSource(ActivityExtensions.ActivitySourceName)
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
            // Old endpoints were only available in Development, so an orchestrator could not
            // perform liveness/readiness checks consistently outside that environment.
            // if (app.Environment.IsDevelopment())
            // {
            //     app.MapHealthChecks("/health");
            //     app.MapHealthChecks("/alive", new HealthCheckOptions
            //     {
            //         Predicate = r => r.Tags.Contains("live")
            //     });
            // }

            // These endpoints are operational endpoints only and are intentionally omitted
            // from OpenAPI. The gateway does not route them to the client application.
            app.MapHealthChecks("/health/ready", new HealthCheckOptions
                {
                    Predicate = _ => true
                })
                .WithMetadata(new ExcludeFromDescriptionAttribute());

            app.MapHealthChecks("/health/live", new HealthCheckOptions
                {
                    Predicate = r => r.Tags.Contains("live")
                })
                .WithMetadata(new ExcludeFromDescriptionAttribute());

            return app;
        }
    }
}
