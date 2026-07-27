namespace  ServiceDefaults.Authorization;

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

public sealed class PermissionPolicyProvider(
    IOptions<AuthorizationOptions> options)
    : IAuthorizationPolicyProvider
{
    public const string Prefix = "Permission:";

    private readonly DefaultAuthorizationPolicyProvider _fallback =
        new(options);

    public Task<AuthorizationPolicy?> GetPolicyAsync(
        string policyName)
    {
        if (!policyName.StartsWith(
                Prefix,
                StringComparison.Ordinal))
        {
            return _fallback.GetPolicyAsync(policyName);
        }

        var permission = policyName[Prefix.Length..];

        var policy = new AuthorizationPolicyBuilder(
                JwtBearerDefaults.AuthenticationScheme)
            .RequireAuthenticatedUser()
            .AddRequirements(
                new PermissionRequirement(permission))
            .Build();

        return Task.FromResult<AuthorizationPolicy?>(policy);
    }

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync()
        => _fallback.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync()
        => _fallback.GetFallbackPolicyAsync();
}