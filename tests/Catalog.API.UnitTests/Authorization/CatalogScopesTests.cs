using System.Security.Claims;

namespace Catalog.API.UnitTests.Authorization;

public class CatalogScopesTests
{
    [Theory]
    [InlineData("catalog.read catalog.write", CatalogScopes.Read)]
    [InlineData("[\"catalog.read\",\"catalog.write\"]", CatalogScopes.Write)]
    public void HasScope_WhenRequiredScopeIsPresent_ReturnsTrue(
        string scopeClaim,
        string requiredScope)
    {
        var user = CreateUser(scopeClaim);

        Assert.True(CatalogScopes.HasScope(user, requiredScope));
    }

    [Fact]
    public void HasScope_WhenRequiredScopeIsMissing_ReturnsFalse()
    {
        var user = CreateUser(CatalogScopes.Read);

        Assert.False(CatalogScopes.HasScope(user, CatalogScopes.Write));
    }

    [Theory]
    [InlineData(CatalogScopes.Read)]
    [InlineData(CatalogScopes.Write)]
    public void HasScope_WhenRequiredPermissionIsPresent_ReturnsTrue(
        string requiredPermission)
    {
        var user = CreateUserWithPermissions(
            CatalogScopes.Read,
            CatalogScopes.Write);

        Assert.True(CatalogScopes.HasScope(user, requiredPermission));
    }

    [Fact]
    public void HasScope_WhenRequiredPermissionIsMissing_ReturnsFalse()
    {
        var user = CreateUserWithPermissions(CatalogScopes.Read);

        Assert.False(CatalogScopes.HasScope(user, CatalogScopes.Write));
    }

    private static ClaimsPrincipal CreateUser(string scopeClaim)
    {
        var identity = new ClaimsIdentity(
            [new Claim("scope", scopeClaim)],
            authenticationType: "Test");

        return new ClaimsPrincipal(identity);
    }

    private static ClaimsPrincipal CreateUserWithPermissions(
        params string[] permissions)
    {
        var identity = new ClaimsIdentity(
            permissions.Select(permission => new Claim("permissions", permission)),
            authenticationType: "Test");

        return new ClaimsPrincipal(identity);
    }
}
