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

    private static ClaimsPrincipal CreateUser(string scopeClaim)
    {
        var identity = new ClaimsIdentity(
            [new Claim("scope", scopeClaim)],
            authenticationType: "Test");

        return new ClaimsPrincipal(identity);
    }
}
