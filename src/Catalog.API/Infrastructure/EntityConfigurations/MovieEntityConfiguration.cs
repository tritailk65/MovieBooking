namespace Catalog.API.Infrastucture.EntityConfigurations
{
    public class MovieEntityConfiguration : IEntityTypeConfiguration<Movie>
    {
        public void Configure(EntityTypeBuilder<Movie> builder)
        {
            builder.ToTable("Movies");

            builder.HasKey(m => m.Id);

            builder.Property(m => m.Title).HasMaxLength(250);
            builder.Property(m => m.Director).HasMaxLength(100);
            builder.Property(m => m.PosterUrl).HasMaxLength(500);

            builder.HasIndex(m => m.Title);

            builder.HasIndex(m => m.ReleaseDate);
        }
    }
}
