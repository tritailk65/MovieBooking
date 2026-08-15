namespace Seat.API.Application.Seats.GetShowtimeSeats;

// TODO  (sau khi xong Order service): 
// Ở luồng đọc nên có cơ chế build lại sơ đồ ghế để phòng trường hợp dữ liệu của seat service bay hơi
// Nên kết hợp dữ liệu tĩnh ở catalog service, và booking service để dựng lại dữ liệu sơ đồ ghế


/// <summary>
/// Luồng đọc trả về danh sách ghế kèm theo trạng thái
/// </summary>
public class GetShowtimeSeatQueryHandler : IRequestHandler<GetShowtimeSeatQuery, ShowtimeSeat>
{
    private ISeatRepository _seatRepository;
    private IRedisLockService _lockService;

    public GetShowtimeSeatQueryHandler(ISeatRepository seatRepository, IRedisLockService lockservice)
    {
        _seatRepository = seatRepository;
        _lockService = lockservice;
    }

    public async Task<ShowtimeSeat> Handle (GetShowtimeSeatQuery request, CancellationToken cancellationToken)
    {
        // Get all set map by
        var seat = await _seatRepository.GetShowtimeSeatsAsync(request.ShowtimeId);

        if (seat is null) return null;


        return seat;
    }
}