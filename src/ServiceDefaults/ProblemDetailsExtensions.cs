using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ServiceDefaults;

public static class ProblemDetailsExtensions
{
    public static IHostApplicationBuilder AddDefaultProblemDetails(
        this IHostApplicationBuilder builder)
    {
        builder.Services.AddProblemDetails(options =>
        {
            options.CustomizeProblemDetails = context =>
            {
                context.ProblemDetails.Extensions["traceId"] =
                    Activity.Current?.TraceId.ToString() ?? context.HttpContext.TraceIdentifier;

                if (context.HttpContext.Items.TryGetValue(
                        CorrelationIdMiddleware.HttpContextItemName,
                        out var correlationId))
                {
                    context.ProblemDetails.Extensions["correlationId"] = correlationId;
                }
            };
        });

        return builder;
    }

    public static WebApplication UseDefaultProblemDetails(this WebApplication app)
    {
        app.UseMiddleware<CorrelationIdMiddleware>();

        app.UseExceptionHandler(new ExceptionHandlerOptions
        {
            StatusCodeSelector = exception => exception switch
            {
                BadHttpRequestException => StatusCodes.Status400BadRequest,
                ArgumentException => StatusCodes.Status400BadRequest,
                KeyNotFoundException => StatusCodes.Status404NotFound,
                TimeoutException => StatusCodes.Status503ServiceUnavailable,
                _ => StatusCodes.Status500InternalServerError
            }
        });

        // Converts empty error responses, including proxy failures, to RFC Problem Details
        // while preserving the original HTTP status code.
        app.UseStatusCodePages();

        return app;
    }
}
