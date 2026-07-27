using Catalog.API.IntegrationEvents.Event;

namespace Catalog.API.Application.Showtimes.Commands.CreateShowtime;

public class CreateShowtimeCommandHandler : IRequestHandler<CreateShowtimeCommand, int>
{
    private readonly CatalogContext _context;
    private readonly ICatalogIntegrationEventService _eventBus;
    private readonly ILogger<CreateShowtimeCommandHandler> _logger;
    //private readonly CatalogServices _service;

    public CreateShowtimeCommandHandler(CatalogContext context, ICatalogIntegrationEventService eventBus, ILogger<CreateShowtimeCommandHandler> logger)
    {
        _context = context;
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task<int> Handle(CreateShowtimeCommand command, CancellationToken cancellationToken)
    {     
        var showtime = new Showtime
        {
            MovieId = command.MovieId,
            HallId = command.HallId,
            CinemaId = command.CinemaId,
            StartTime = command.StartTime,
            EndTime = command.EndTime,
            BasePrice = command.BasePrice
        };

        // Danh sách ghế được lưu trong table Seats, mỗi ghế sẽ có HallId để biết nó thuộc phòng chiếu nào
        // Khi tạo showtime, sẽ lấy tất cả ghế của phòng chiếu đó và gửi kèm trong integration event để Seat Service tạo các seat availability tương ứng

        var seats = await _context.Seats
            .Where(s => s.HallId == command.HallId)
            .Select(s => s.SeatCode) // Ví dụ: "A1", "A2", ...
            .ToListAsync(cancellationToken);

        _context.Showtimes.Add(showtime);

        //Reset Hilo id block ??
        // Vậy mỗi lầm create showtime sẽ phải chạy lại câu sql dưới đây ?
        // await _context.Database.ExecuteSqlRawAsync(
        //     "SELECT setval('showtimeseq', (SELECT MAX(\"Id\") FROM \"Showtimes\"));"
        // );

        // Nếu chỉ làm tuần tự thì sẽ không thể xử lý được tính huống đã lưu db nhưng rớt message lại
        // await _context.SaveChangesAsync(cancellationToken); 
        // _logger.LogInformation("Showtime {ShowtimeId} has been saved to DB.", showtime.Id);

        var integrationEvent = new ShowtimeCreatedIntegrationEvent(
            showtime.Id, 
            showtime.HallId, 
            showtime.MovieId, 
            showtime.StartTime,
            showtime.EndTime,
            showtime.BasePrice,
            seats // nếu danh sách ghế quá lớn thì sao, có nên gửi qua message bus không? 
            // Nếu quá lớn thì có thể chỉ gửi thông tin về số lượng ghế và để Seat Service tự lấy danh sách ghế dựa trên HallId, 
            // nhưng như vậy sẽ làm tăng độ coupling giữa 2 service, vì Seat Service sẽ phải biết cách lấy danh sách ghế dựa trên HallId, 
            // trong khi nếu gửi danh sách ghế qua message bus thì Seat Service chỉ cần nhận và xử lý mà không cần biết cách lấy danh sách ghế từ đâu
        );

        await _eventBus.SaveEventAndCatalogContextChangesAsync(integrationEvent);
        await _eventBus.PublishThroughEventBusAsync(integrationEvent);

        return showtime.Id;
    }
}
