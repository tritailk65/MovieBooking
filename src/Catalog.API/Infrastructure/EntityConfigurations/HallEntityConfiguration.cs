using Catalog.API.Domain.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.API.Infrastucture.EntityConfigurations
{
    public class HallEntityConfiguration : IEntityTypeConfiguration<Hall>
    {
        public void Configure(EntityTypeBuilder<Hall> builder)
        {
            builder.ToTable("Halls");

            builder.HasKey(h => h.Id);
            builder.Property(h => h.Name).HasMaxLength(100);

            var navigation = builder.Metadata.FindNavigation(nameof(Hall.Seats));
            navigation?.SetPropertyAccessMode(PropertyAccessMode.Field);

            builder.HasMany(h => h.Seats)
                .WithOne()
                .HasForeignKey(s => s.HallId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
