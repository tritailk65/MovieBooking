namespace Catalog.API.Infrastucture.EntityConfigurations
{
    public class SeatEntityConfiguration : IEntityTypeConfiguration<Seat>
    {
        public void Configure(EntityTypeBuilder<Seat> builder)
        {
            builder.ToTable("Seats");

            builder.HasKey(s => s.Id);

            builder.Property(s => s.Row).HasMaxLength(5);

            builder.Ignore(s => s.SeatCode);

            // Composite Unique Index (Chống trùng lặp ghế vật lý)
            builder.HasIndex(s => new { s.HallId, s.Row, s.Number }).IsUnique();
        }
    }
}
