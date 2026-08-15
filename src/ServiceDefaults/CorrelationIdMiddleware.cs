using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace ServiceDefaults;

public sealed class CorrelationIdMiddleware(
    RequestDelegate next,
    ILogger<CorrelationIdMiddleware> logger)
{
    public const string HeaderName = "X-Correlation-Id";
    public const string HttpContextItemName = "CorrelationId";

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = TryGetValidCorrelationId(context.Request.Headers, out var clientCorrelationId)
            ? clientCorrelationId
            : Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString("N");

        context.Items[HttpContextItemName] = correlationId;
        context.Request.Headers[HeaderName] = correlationId;
        Activity.Current?.SetTag("correlation.id", correlationId);

        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        using (logger.BeginScope(new Dictionary<string, object>
               {
                   [HttpContextItemName] = correlationId
               }))
        {
            await next(context);
        }
    }

    private static bool TryGetValidCorrelationId(
        IHeaderDictionary headers,
        out string correlationId)
    {
        correlationId = string.Empty;

        if (!headers.TryGetValue(HeaderName, out StringValues values) || values.Count != 1)
        {
            return false;
        }

        var candidate = values[0];
        if (string.IsNullOrWhiteSpace(candidate) || candidate.Length > 128)
        {
            return false;
        }

        if (candidate.Any(character =>
                !char.IsLetterOrDigit(character) && character is not '-' and not '_' and not '.'))
        {
            return false;
        }

        correlationId = candidate;
        return true;
    }
}
