using Catalog.API.Infrastucture.EntityConfigurations;
using IntergrationEventLog;

namespace Catalog.API.Infrastucture
{
    /// <summary>
    /// dotnet ef migrations add --startup-project Ordering.API --context OrderingContext [migration-name]  --output-dir <Path/To/Directory>
    /// 
    /// dotnet ef migrations add InitialCreate -o Data/Migrations
    /// 
    /// dotnet ef migrations add <MigrationName> --output-dir <Path/To/Directory>
    /// 
    /// Add-Migration YourMigrationName -OutputDir "Your/Custom/Folder"
    /// 
    /// </summary>
    /// 
    public class CatalogContext : DbContext
    {
        public CatalogContext(DbContextOptions<CatalogContext> options, IConfiguration configuration) : base(options) { }

        public  DbSet<Cinema> Cinemas { get; set; }
        public  DbSet<Vendor> Vendors { get; set; }
        public  DbSet<Hall>Halls { get; set; }
        public  DbSet<Seat> Seats { get; set; }
        public  DbSet<Movie> Movies { get; set; }
        public   DbSet<Showtime> Showtimes { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.ApplyConfiguration(new VendorEntityTypeConfiguration());
            builder.ApplyConfiguration(new CinemaEntityConfiguration());
            builder.ApplyConfiguration(new HallEntityConfiguration());
            builder.ApplyConfiguration(new SeatEntityConfiguration());
            builder.ApplyConfiguration(new MovieEntityConfiguration());
            builder.ApplyConfiguration(new ShowtimeEntityConfiguration());

            // Add the outbox table to this context
            builder.UseIntegrationEventLogs();
        }
    }
}
