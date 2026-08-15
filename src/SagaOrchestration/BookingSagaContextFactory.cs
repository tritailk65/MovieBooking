using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SagaOrchestration;

public sealed class BookingSagaContextFactory
    : IDesignTimeDbContextFactory<BookingSagaContext>
{
    public BookingSagaContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable(
                "ConnectionStrings__sagadb")
            ?? "Host=localhost;Port=5432;Database=sagadb;" +
               "Username=postgres;Password=postgres";

        var options = new DbContextOptionsBuilder<BookingSagaContext>()
            .UseNpgsql(
                connectionString,
                postgres => postgres.MigrationsAssembly(
                    typeof(BookingSagaContext).Assembly.FullName))
            .Options;

        return new BookingSagaContext(options);
    }
}