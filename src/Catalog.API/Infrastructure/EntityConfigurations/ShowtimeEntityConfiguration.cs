namespace Catalog.API.Infrastucture.EntityConfigurations
{
    public class ShowtimeEntityConfiguration : IEntityTypeConfiguration<Showtime>
    {
        public void Configure(EntityTypeBuilder<Showtime> builder)
        {
            builder.ToTable("Showtimes");

            builder.HasKey(s => s.Id);

            builder.Property(s => s.Id).UseHiLo("showtimeseq");

            builder.Property(s => s.BasePrice)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            builder.HasIndex(s => new { s.MovieId, s.StartTime });

            builder.HasIndex(s => new { s.HallId, s.StartTime });
        }
    }
}
