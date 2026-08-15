namespace Catalog.API.Infrastucture.EntityConfigurations
{
    public class CinemaEntityConfiguration : IEntityTypeConfiguration<Cinema>
    {
        public void Configure(EntityTypeBuilder<Cinema> builder)
        {
            builder.ToTable("Cinemas");

            builder.HasKey(c => c.Id);

            builder.Property(c => c.Name).HasMaxLength(200);
            builder.Property(c => c.Address).HasMaxLength(500);
            builder.Property(c => c.City).HasMaxLength(100);


            builder.HasIndex(c => c.City);
        }
    }
}
