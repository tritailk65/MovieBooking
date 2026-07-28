using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using ServiceDefaults.Authorization;

namespace Catalog.API.UnitTests.Authorization;

public class PermissionAuthorizationHandlerTests
{
    [Theory]
    [InlineData("scope", "catalog.read catalog.write", "catalog.read")]
    [InlineData("scp", "[\"catalog.read\",\"catalog.write\"]", "catalog.write")]
    [InlineData("permissions", "catalog.read", "catalog.read")]
    public async Task HandleAsync_WhenRequiredPermissionIsPresent_Succeeds(
        string claimType,
        string claimValue,
        string requiredPermission)
    {
        var context = CreateContext(claimType, claimValue, requiredPermission);
        var handler = new PermissionAuthorizationHandler();

        await handler.HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }

    [Theory]
    [InlineData("scope")]
    [InlineData("scp")]
    [InlineData("permissions")]
    public async Task HandleAsync_WhenRequiredPermissionIsMissing_DoesNotSucceed(
        string claimType)
    {
        var context = CreateContext(
            claimType,
            "catalog.read",
            "catalog.write");
        var handler = new PermissionAuthorizationHandler();

        await handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    private static AuthorizationHandlerContext CreateContext(
        string claimType,
        string claimValue,
        string requiredPermission)
    {
        var identity = new ClaimsIdentity(
            [new Claim(claimType, claimValue)],
            authenticationType: "Test");
        var requirement = new PermissionRequirement(requiredPermission);

        return new AuthorizationHandlerContext(
            [requirement],
            new ClaimsPrincipal(identity),
            resource: null);
    }
}
