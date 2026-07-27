namespace ServiceDefaults.Authorization;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

public sealed class PermissionAuthorizationHandler
    : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var hasPermission = HasClaimValue(
            context.User,
            ["permissions"],
            requirement.Permission);

        var hasScope = HasClaimValue(
            context.User,
            ["scope", "scp"],
            requirement.Permission);

        if (hasPermission && hasScope)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }

    private static bool HasClaimValue(
        ClaimsPrincipal user,
        IEnumerable<string> claimTypes,
        string requiredValue)
    {
        return claimTypes
            .SelectMany(user.FindAll)
            .SelectMany(claim => claim.Value.Split(
                [' ', ',', '[', ']', '"'],
                StringSplitOptions.RemoveEmptyEntries |
                StringSplitOptions.TrimEntries))
            .Contains(requiredValue, StringComparer.Ordinal);
    }
}