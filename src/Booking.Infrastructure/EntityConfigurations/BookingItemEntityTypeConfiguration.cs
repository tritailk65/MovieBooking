
using BookingService.Domain.AggregateModel.BookingAggregates;

namespace BookingService.Infrastructure.EntityConfigurations;

public class BookingItemEntityTypeConfiguration : IEntityTypeConfiguration<BookingItem>
{
    public void Configure(EntityTypeBuilder<BookingItem> bookingItemTypeConfiguration)
    {
        bookingItemTypeConfiguration.ToTable("bookingitems");

        bookingItemTypeConfiguration.Ignore(b => b.DomainEvents);

        bookingItemTypeConfiguration.Property(b => b.Id).UseHiLo("orderitemseq");

    }
}