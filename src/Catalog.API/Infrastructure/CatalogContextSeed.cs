using System.Text.Json;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Catalog.API.Infrastucture
{
    public partial class CatalogContextSeed(
        IWebHostEnvironment env, 
        IOptions<CatalogOptions> settings,
        ILogger<CatalogContextSeed> logger) : IDbSeeder<CatalogContext>
    {
        public async Task SeedAsync(CatalogContext context)
        {
            var useCustomizationData = settings.Value.UseCustomizationData;
            var contentRootPath = env.ContentRootPath;
            var picturePath = env.WebRootPath;

            context.Database.OpenConnection();
            ((NpgsqlConnection)context.Database.GetDbConnection()).ReloadTypes();

            var seedDataPath = Path.Combine(contentRootPath, "Setup");

            try
            {
                var vendorSeedData = JsonSerializer.Deserialize<List<VendorSourceEntry>>(await File.ReadAllTextAsync(Path.Combine(seedDataPath, "vendors.json")));
                if (!context.Vendors.Any())
                {
                    context.Vendors.RemoveRange(context.Vendors);
                    var vendors = vendorSeedData.Where(v => v.Name != null).Select(v => new Vendor(v.Name)
                    {
                        Id = Convert.ToInt32(v.Id),
                        Name = v.Name,
                        LogoUrl = v.LogoUrl
                    });

                    await context.Vendors.AddRangeAsync(vendors);
                    await context.SaveChangesAsync();
                }

                if (!context.Cinemas.Any())
                {
                    context.Cinemas.RemoveRange(context.Cinemas);
                    var cinema = vendorSeedData.SelectMany(x => x.Cinemas.DistinctBy(c => c.Id)).Where(c => c.Name != null && c.Address != null && c.City != null);
                    await context.Cinemas.AddRangeAsync(cinema.Select(c => new Cinema(c.Name)
                    {
                        Id = Convert.ToInt32(c.Id),
                        Name = c.Name,
                        VendorId = Convert.ToInt32(c.VendorId),
                        Address = c.Address,
                        City = c.City
                    }));

                    await context.SaveChangesAsync();
                }

                if (!context.Halls.Any())
                {
                    context.Halls.RemoveRange(context.Halls);
                    var halls = vendorSeedData.SelectMany(x => x.Cinemas.DistinctBy(c => c.Id)).SelectMany(x => x.Halls.DistinctBy(x => x.Id)).Where(h => h.Name != null);
                    await context.Halls.AddRangeAsync(halls.Select(h => new Hall(h.Name)
                    {
                        CinemaId = Convert.ToInt32(h.CinemaId),
                        Id = Convert.ToInt32(h.Id),
                        Name = h.Name
                    }));

                    await context.SaveChangesAsync();
                }

                if (!context.Seats.Any())
                {
                    context.Seats.RemoveRange(context.Seats);
                    var seats = vendorSeedData.SelectMany(x => x.Cinemas).SelectMany(x => x.Halls).SelectMany(x => x.Seats.DistinctBy(v => v.Id)).Where(v => v.Row != null);
                    await context.Seats.AddRangeAsync(seats.Select(s => new Seat(s.Row, s.Number)
                    {
                        Id = Convert.ToInt32(s.Id),
                        Row = s.Row,
                        Number = Convert.ToInt32(s.Number),
                        HallId = Convert.ToInt32(s.HallId),
                    }));

                    await context.SaveChangesAsync();
                }
 
                if (!context.Movies.Any())
                {
                    context.Movies.RemoveRange(context.Movies);
                    var movieData = await File.ReadAllTextAsync(Path.Combine(seedDataPath, "movies.json"));
                    var movies = JsonSerializer.Deserialize<List<Movie>>(movieData);
                    if (movies != null)
                    {
                        context.Movies.AddRange(movies);
                        await context.SaveChangesAsync();
                        logger.LogInformation("Seeded {NumMovie} movies", context.Movies.Count());

                    }
                }

                // if (!context.Showtimes.Any())
                // {
                //     context.Showtimes.RemoveRange(context.Showtimes);
                //     var showtimeData = await File.ReadAllTextAsync(Path.Combine(seedDataPath, "showtimes.json"));
                //     var showtimes = JsonSerializer.Deserialize<List<Showtime>>(showtimeData);
                //     if (showtimes != null)
                //     {
                //         context.Showtimes.AddRange(showtimes);
                //         await context.SaveChangesAsync();
                //         logger.LogInformation("Seeded {NumVendor} vendors and {Cinema} cinemas", context.Vendors.Count(), context.Cinemas.Count());

                //     }
                // }

            }
            catch (Exception ex)
            {
                logger.LogError("Seeding data error with message: " + ex.ToString());

            }
        }

        public class VendorSourceEntry
        {
            public string Id {  get; set; }
            public string Name { get; set; }
            public string LogoUrl {  get; set; }
            public bool IsActive {  get; set; }
            public List<CinemaSourceEntry> Cinemas { get; set; }
        }

        public class CinemaSourceEntry
        {
            public string Id { get; set; }
            public string VendorId { get; set; }
            public string Name { get; set; }
            public string Address { get; set; }
            public string City { get; set; }

            public List<HallSourceEntry> Halls { get; set; }
        }

        public class HallSourceEntry
        {
            public string Id { get; set; }
            public string CinemaId { get; set; }
            public string Name { get; set; }
            public List<SeatSourceEntry> Seats {  get; set; }
        }

        public class SeatSourceEntry
        {
            public string Id { get; set; }
            public string HallId { get; set; }
            public string Row { get; set; }
            public int Number { get; set; }
        }
    
    }
}


