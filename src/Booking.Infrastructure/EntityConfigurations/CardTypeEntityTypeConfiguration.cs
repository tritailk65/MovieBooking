
namespace BookingService.Infrastructure.EntityConfigurations;

public class CardEntityTypeConfiguration : IEntityTypeConfiguration<CardType>
{
    public void Configure(EntityTypeBuilder<CardType> cardTypeConfiguration)
    {
        cardTypeConfiguration.ToTable("cardtypes");
        cardTypeConfiguration.Property(ct => ct.Id).ValueGeneratedNever();
        cardTypeConfiguration.Property(ct => ct.Name).HasMaxLength(200);

    }
}