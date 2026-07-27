namespace Seat.API.IntegrationEvents.EventHandlers;

public class ShowtimeCreatedIntegrationEventHandler : IIntegrationEventHandler<ShowtimeCreatedIntegrationEvent>
{
    private readonly ISeatRepository _seatRepository;
    private readonly ILogger<ShowtimeCreatedIntegrationEventHandler> _logger;

    public ShowtimeCreatedIntegrationEventHandler(ISeatRepository seatRepository, ILogger<ShowtimeCreatedIntegrationEventHandler> logger)
    {
        _seatRepository = seatRepository;
        _logger = logger;
    }

    public async Task Handle(ShowtimeCreatedIntegrationEvent @event)
    {
        _logger.LogInformation("Received ShowtimeCreatedIntegrationEvent: {ShowtimeId}, {HallId}, {MovieId}, {StartTime}, {EndTime}, {BasePrice}",
            @event.ShowtimeId, @event.HallId, @event.MovieId, @event.StartTime, @event.EndTime, @event.BasePrice);

        try
        {
            var seats = @event.Seats.Select( seatId => new Domain.Entities.Seat
            {
                ShowtimeId = @event.ShowtimeId,
                SeatId = seatId,
                SeatStatus = SeatStatus.Available,
                BasePrice = @event.BasePrice
            });

            var showtimeSeats = new ShowtimeSeat
            {
                ShowtimeId = @event.ShowtimeId,
                Seats = seats
            };

            // If seat map don't have any data, skip
            if (showtimeSeats == null)
            {
                _logger.LogError( "Seat map null: {ShowtimeId}", @event.ShowtimeId);
            }

            // Save data to redis 
            await _seatRepository.InitializeSeatsAsync(@event.ShowtimeId, showtimeSeats);

            _logger.LogInformation( "Create seat map ok: {ShowtimeId}", @event.ShowtimeId);

        } 
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling ShowtimeCreatedIntegrationEvent: {ShowtimeId}", @event.ShowtimeId);
        }
    }
}
