namespace Catalog.API.Domain.Entities
{
    public class Showtime
    {
        public int Id { get; set; }
        public int CinemaId {get; set;}
        public int MovieId { get; set; }
        public int HallId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public decimal BasePrice { get; set; }

        public Showtime() {}

        public Showtime(int movieId, int cinemaId, int hallId, DateTime startTime, DateTime endTime, decimal basePrice)
        {
            CinemaId = cinemaId;
            MovieId = movieId;
            HallId = hallId;
            StartTime = startTime;
            EndTime = endTime;
            BasePrice = basePrice;
        }
    }
}
