namespace SagaOrchestration;

using MassTransit.EntityFrameworkCoreIntegration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

public class BookingSagaContext : SagaDbContext
{

    public BookingSagaContext(DbContextOptions<BookingSagaContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.AddInboxStateEntity();
        builder.AddOutboxMessageEntity();
        builder.AddOutboxStateEntity();
    }


    protected override IEnumerable<ISagaClassMap> Configurations => new[] { new BookingClassMap() };
}
