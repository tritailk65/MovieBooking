
using BookingService.Domain.AggregateModel.BookingAggregates;

namespace BookingService.Infrastructure.EntityConfigurations;

public class PaymentMethodEntityTypeConfiguration : IEntityTypeConfiguration<PaymentMethod>
{
    public void Configure(EntityTypeBuilder<PaymentMethod> paymentMethodTypeConfiguration)
    {
        paymentMethodTypeConfiguration.ToTable("paymentmethods");

        paymentMethodTypeConfiguration.Ignore(x => x.DomainEvents);

        paymentMethodTypeConfiguration.Property(x => x.CardHolderName).HasMaxLength(200);
        
        paymentMethodTypeConfiguration.Property(x => x.Alias).HasMaxLength(200);

        paymentMethodTypeConfiguration.Property(x => x.SecurityNumber).HasMaxLength(25);

        paymentMethodTypeConfiguration.Property(x => x.Expiration).HasMaxLength(25);

        paymentMethodTypeConfiguration.HasOne(x => x.CardType).WithMany().HasForeignKey("_cardTypeId");

    }
}