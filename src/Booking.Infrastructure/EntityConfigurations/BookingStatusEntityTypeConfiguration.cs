using BookingService.Domain.AggregateModel.BookingAggregates;

namespace BookingService.Infrastructure.EntityConfigurations;

public class BookingStatusEntityTypeConfiguration : IEntityTypeConfiguration<BookingStatus>
{
    public void Configure(EntityTypeBuilder<BookingStatus> bookingStatusConfiguration)
    {
        bookingStatusConfiguration.ToTable("bookingstatus");

        bookingStatusConfiguration.HasKey(b => b.Id);

        bookingStatusConfiguration.Property(b => b.Id).ValueGeneratedNever();

        bookingStatusConfiguration.Property(b => b.Name)
            .HasMaxLength(200)
            .IsRequired();

        bookingStatusConfiguration.HasData(
            BookingStatus.Submitted,
            BookingStatus.AwaitingSeatValidation,
            BookingStatus.SeatConfirmed,
            BookingStatus.Paid,
            BookingStatus.Cancelled);
    }
}
