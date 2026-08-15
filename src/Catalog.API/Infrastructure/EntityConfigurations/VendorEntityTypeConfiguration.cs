using Catalog.API.Domain.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.API.Infrastucture.EntityConfigurations
{
    public class VendorEntityTypeConfiguration : IEntityTypeConfiguration<Vendor>
    {
        public void Configure(EntityTypeBuilder<Vendor> builder)
        {
            builder.ToTable("Vendors");

            builder.HasKey(v => v.Id);

            builder.Property(v => v.Name).HasMaxLength(100);

            builder.Property(v => v.LogoUrl).HasMaxLength(500);

            var navigation = builder.Metadata.FindNavigation(nameof(Vendor.Cinemas));
            navigation?.SetPropertyAccessMode(PropertyAccessMode.Field);

            builder.HasMany(v => v.Cinemas)
                .WithOne()
                .HasForeignKey(c => c.VendorId)
                .OnDelete(DeleteBehavior.Cascade); 
        }
    }
}
