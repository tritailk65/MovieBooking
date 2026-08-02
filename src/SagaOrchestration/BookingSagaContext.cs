namespace Shared.Infrastructure.OrderSaga;

using MassTransit.EntityFrameworkCoreIntegration;
using Microsoft.EntityFrameworkCore;

public class BookingSagaContext : SagaDbContext
{
    public BookingSagaContext(DbContextOptions<BookingSagaContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

    }


    protected override IEnumerable<ISagaClassMap> Configurations => new[] { new BookingClassMap() };
}
