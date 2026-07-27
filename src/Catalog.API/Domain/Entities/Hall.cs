namespace Catalog.API.Domain.Entities
{
    public class Hall
    {
        public int Id { get; set; }
        public int CinemaId { get; set; }
        [Required]
        public string Name { get; set; } = default!;
        public int TotalSeats { get; private set; }
        public IReadOnlyCollection<Seat> Seats => _seats.AsReadOnly();

        private readonly List<Seat> _seats = new();

        public Hall(string name)
        {
            Name = name;
        }

        public void AddSeat(Seat seat) => _seats.Add(seat);

        public int GetTotalSeat() => _seats.Count();
    }
}
