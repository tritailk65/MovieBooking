namespace Catalog.API.Domain.Entities
{
    public class Movie
    {
        public int Id { get; set; }
        [Required]
        public string Title { get; set; } = default!;
        public string Description { get; set; } = default!;
        public string Director { get; set; } = default!;
        public string Cast { get; set; } = default!;
        public int DurationMinutes { get; set; }
        public DateTime ReleaseDate { get; set; }
        public string PosterUrl { get; set; } = default!;
        public string TrailerUrl { get; set; } = default!;
        public bool IsShowing { get; set; }

        public Movie(string title, string description, int durationMinutes, DateTime releaseDate)
        {
            Title = title;
            Description = description;
            DurationMinutes = durationMinutes;
            ReleaseDate = releaseDate;
            IsShowing = true;
        }

        // Parameterless constructor for EF Core
        public Movie()
        {   }
    }
}
