namespace Catalog.API.Domain.Entities
{
    public class Seat
    {
        public int Id { get; set; }
        public int HallId { get; set; }
        [Required]
        public string Row { get; set; } = default!;
        public int Number { get;  set; }
        public string SeatCode => $"{Row}{Number}";
        public SeatType Type { get; private set; }

        public Seat(string row, int number)
        {
            Row = row;
            Number = number;
        }
    }
}
