namespace SagaOrchestration.Contracts;

public record ShowtimeCreatedIntegrationEvent : IntegrationEvent
{
    public int ShowtimeId {get; init;}
    public int HallId {get; init;}
    public int MovieId {get; init;}
    public DateTime StartTime {get; init;}
    public DateTime EndTime {get; init;}
    public decimal BasePrice {get; init;}

    public IEnumerable<string> Seats { get; set; } = new List<string>();

    public ShowtimeCreatedIntegrationEvent(int showtimeId, int hallId, int movieId, DateTime startTime, DateTime endTime, decimal basePrice, IEnumerable<string> seats)
    {
        ShowtimeId = showtimeId;
        HallId = hallId;
        MovieId = movieId;
        StartTime = startTime;
        EndTime = endTime;
        BasePrice = basePrice;
        Seats = seats;
    }
}