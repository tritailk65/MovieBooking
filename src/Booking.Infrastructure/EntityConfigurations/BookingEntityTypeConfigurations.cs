namespace BookingService.Infrastructure.EntityConfigurations;

public class BookingEntityTypeConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> bookingTypeConfiguration)
    {
        bookingTypeConfiguration.ToTable("bookings");

        bookingTypeConfiguration.Property(b => b.UserId).IsRequired();

        bookingTypeConfiguration.Property(b => b.ShowtimeId).IsRequired();

        bookingTypeConfiguration.Ignore(b => b.DomainEvents);

        bookingTypeConfiguration.Property(b => b.Id).UseHiLo("bookingseg");

        bookingTypeConfiguration.Property(b => b.ReservationId).IsRequired();

        // Lưu hardcode enum trong db để quản lý 
        bookingTypeConfiguration
            .HasOne(b => b.BookingStatus)
            .WithMany()
            .HasForeignKey("_bookingStatusId");

        bookingTypeConfiguration.Property(b => b.PaymentId).HasColumnName("PaymentMethodId");

        // Don hang co the thanh toan nhieu hinh thuc
        bookingTypeConfiguration.HasOne<PaymentMethod>().WithMany().HasForeignKey(x => x.PaymentId).OnDelete(DeleteBehavior.Restrict);

    }
}
