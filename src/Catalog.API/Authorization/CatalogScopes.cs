using System.Security.Claims;
using Microsoft.Extensions.DependencyInjection;

namespace Catalog.API;

public static class CatalogScopes
{
    public const string Read = "catalog.read";
    public const string Write = "catalog.write";

    public static bool HasScope(ClaimsPrincipal user, string requiredScope)
    {
        return user.FindAll("scope")
            .Concat(user.FindAll("scp"))
            .SelectMany(claim => claim.Value.Split(
                [' ', ',', '[', ']', '"'],
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Contains(requiredScope, StringComparer.Ordinal);
    }

    public static IServiceCollection AddCatalogAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy(Read, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireAssertion(context => HasScope(context.User, Read));
            });

            options.AddPolicy(Write, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireAssertion(context => HasScope(context.User, Write));
            });
        });

        return services;
    }
}
