namespace Seat.API.Infrastructure.Redis;

public class SeatRedisRepository : ISeatRepository
{
    private readonly IConnectionMultiplexer _redis;
    
    private readonly IDatabase _db;

    public SeatRedisRepository(IConnectionMultiplexer redis)
    {
        _redis = redis;
        _db = _redis.GetDatabase();
    }

    private string GetShowtimeKey(int showtimeId) => $"showtime:{showtimeId}:seats";
    private string GetReservationKey(int showtimeId, string userId) => $"reservation:showtime:{showtimeId}:user:{userId}:seats";
    private const string ReservationHashField = "reservation";

    public async Task InitializeSeatsAsync(int showtimeId, ShowtimeSeat seats)
    {
        var key = GetShowtimeKey(showtimeId);

        var entries = seats.Seats.Select(seat => new HashEntry
        (
            seat.SeatId,
            JsonSerializer.Serialize(seat)
        )).ToArray();

        await _db.HashSetAsync(key, entries);
    }

    public async Task<Domain.Entities.Seat> GetSeatHashAsync(int showtimeId, string seatId)
    {
        var seatMapKey = GetShowtimeKey(showtimeId);
        var seatMapData = await _db.HashGetAsync(seatMapKey, seatId);
        if (!seatMapData.HasValue) return null;

        return JsonSerializer.Deserialize<Domain.Entities.Seat>(seatMapData.ToString());
    }

    public async Task<SeatReservation> GetSeatReservationHashAsync(int showtimeId, string userId)
    {
        var seatResKey = GetReservationKey(showtimeId, userId);

        var reservationData = await _db.HashGetAsync(seatResKey, ReservationHashField);
        if (!reservationData.HasValue) return null;

        return JsonSerializer.Deserialize<SeatReservation>(reservationData.ToString());
    }

    public async Task SetSeatReservationHashAsync(SeatReservation seatReservation)
    {
        var seatResKey = GetReservationKey(seatReservation.ShowtimeId, seatReservation.UserId);

        await _db.HashSetAsync(seatResKey, ReservationHashField, JsonSerializer.Serialize(seatReservation));

        // chỉnh lại TTL khi người dùng thêm vào 1 ghế
        var ttl = seatReservation.ExpiresAt - DateTime.UtcNow;
        if (ttl > TimeSpan.Zero)
            await _db.KeyExpireAsync(seatResKey, ttl);
    }

    //Remove 1 ghế ra khỏi reservation
    public async Task RemoveSeatFromReservationHashAsync(int showtimeId, string userId, string seatId)
    {
        var seatReservation = await GetSeatReservationHashAsync(showtimeId, userId);
        if (seatReservation is null) return;

        var remainingSeatIds = seatReservation.SeatIds
            .Where(x => x != seatId)
            .ToArray();

        var seatResKey = GetReservationKey(showtimeId, userId);

        if (remainingSeatIds.Length == 0)
        {
            await _db.KeyDeleteAsync(seatResKey);
            return;
        }

        seatReservation.SeatIds = remainingSeatIds;
        await SetSeatReservationHashAsync(seatReservation);
    }

    public async Task<ShowtimeSeat> GetShowtimeSeatsAsync(int showtimeId)
    {
        var key = GetShowtimeKey(showtimeId);
        var entries = await _db.HashGetAllAsync(key);

        if (entries.Length == 0)
            return null;

        var seats = entries
            .Select(x => JsonSerializer.Deserialize<Domain.Entities.Seat>(x.Value.ToString()))
            .Where(x => x is not null)
            .Select(x => x!)
            .ToArray();

        return new ShowtimeSeat
        {
            ShowtimeId = showtimeId,
            Seats = seats
        };
    }

    public async Task SetSeatHashAysnc(int showtimeId, string seatId, string value)
    {
        var seatMapKey = GetShowtimeKey(showtimeId);
        await _db.HashSetAsync(seatMapKey, seatId, value);
    }

    public async Task<bool> ReleaseSeatReservationHashAsync(int showtimeId, string userId)
    {
        var seatResKey = GetReservationKey(showtimeId, userId);
        return await _db.KeyDeleteAsync(seatResKey);
    }
}
