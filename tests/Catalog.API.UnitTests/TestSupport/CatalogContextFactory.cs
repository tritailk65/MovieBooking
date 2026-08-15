using Catalog.API.Infrastucture;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Catalog.API.UnitTests.TestSupport;

public static class CatalogContextFactory
{
    public static CatalogContext Create()
    {
        var options = new DbContextOptionsBuilder<CatalogContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var configuration = new ConfigurationBuilder().Build();

        return new CatalogContext(options, configuration);
    }
}