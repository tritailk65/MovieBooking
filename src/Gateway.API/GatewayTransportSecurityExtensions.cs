using System.Net;
using Microsoft.AspNetCore.HttpOverrides;

namespace Gateway.API;

public static class GatewayTransportSecurityExtensions
{
    public static WebApplicationBuilder AddGatewayTransportSecurity(
        this WebApplicationBuilder builder)
    {
        var forwardedHeaders = builder.Configuration.GetSection("ForwardedHeaders");
        var knownProxyValues = forwardedHeaders
            .GetSection("KnownProxies")
            .Get<string[]>() ?? [];
        var knownNetworkValues = forwardedHeaders
            .GetSection("KnownIPNetworks")
            .Get<string[]>() ?? [];
        var allowedForwardedHosts = forwardedHeaders
            .GetSection("AllowedHosts")
            .Get<string[]>() ?? [];

        ValidateEnvironmentConfiguration(
            builder,
            knownProxyValues,
            knownNetworkValues,
            allowedForwardedHosts);

        builder.Services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor |
                                       ForwardedHeaders.XForwardedHost |
                                       ForwardedHeaders.XForwardedProto;
            options.ForwardLimit = 1;

            options.KnownProxies.Clear();
            options.KnownIPNetworks.Clear();
            options.AllowedHosts.Clear();

            foreach (var value in knownProxyValues)
            {
                if (!IPAddress.TryParse(value, out var address))
                {
                    throw new InvalidOperationException(
                        $"ForwardedHeaders:KnownProxies contains an invalid IP address: {value}");
                }

                options.KnownProxies.Add(address);
            }

            foreach (var value in knownNetworkValues)
            {
                if (!System.Net.IPNetwork.TryParse(value, out var network))
                {
                    throw new InvalidOperationException(
                        $"ForwardedHeaders:KnownIPNetworks contains an invalid CIDR network: {value}");
                }

                options.KnownIPNetworks.Add(network);
            }

            foreach (var host in allowedForwardedHosts)
            {
                options.AllowedHosts.Add(host);
            }
        });

        builder.Services.AddHttpsRedirection(options =>
        {
            options.RedirectStatusCode = StatusCodes.Status308PermanentRedirect;
        });

        builder.Services.AddHsts(options =>
        {
            options.MaxAge = TimeSpan.FromDays(
                builder.Configuration.GetValue("Https:HstsMaxAgeDays", 30));
            options.IncludeSubDomains = builder.Configuration.GetValue<bool>(
                "Https:HstsIncludeSubDomains");
            options.Preload = builder.Configuration.GetValue<bool>("Https:HstsPreload");
        });

        return builder;
    }

    private static void ValidateEnvironmentConfiguration(
        WebApplicationBuilder builder,
        IReadOnlyCollection<string> knownProxies,
        IReadOnlyCollection<string> knownNetworks,
        IReadOnlyCollection<string> allowedForwardedHosts)
    {
        if (builder.Environment.IsDevelopment())
        {
            return;
        }

        var allowedHosts = builder.Configuration["AllowedHosts"];
        if (string.IsNullOrWhiteSpace(allowedHosts) || allowedHosts == "*")
        {
            throw new InvalidOperationException(
                "AllowedHosts must be explicitly configured in Staging and Production.");
        }

        if (knownProxies.Count == 0 && knownNetworks.Count == 0)
        {
            throw new InvalidOperationException(
                "At least one trusted proxy or network must be configured in Staging and Production.");
        }

        if (allowedForwardedHosts.Count == 0 || allowedForwardedHosts.Contains("*"))
        {
            throw new InvalidOperationException(
                "ForwardedHeaders:AllowedHosts must contain explicit hosts in Staging and Production.");
        }
    }
}
