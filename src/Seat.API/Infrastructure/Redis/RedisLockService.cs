using System.Text.Json;
using Seat.API.Domain.Interfaces;
using StackExchange.Redis;

namespace Seat.API.Infrastructure.Redis;

public class RedisLockService : IRedisLockService
{
    private readonly IDatabase _database;

    public RedisLockService(IConnectionMultiplexer redis)
    {
        _database = redis.GetDatabase();
    }

    private string GetLockKey(int showtimeId, string seatId) => $"lock:showtime:{showtimeId}:seat:{seatId}";
    private string GetMutexKey(int showtimeId, string seatId) => $"mutex:showtime:{showtimeId}:seat:{seatId}";

    public async Task<string> AcquireLockAsync(int showtimeId, string seatId, TimeSpan expiration)
    {
        var mutexKey = GetMutexKey(showtimeId, seatId);
        var mutexToken = Guid.NewGuid().ToString();
        var result = await _database.StringSetAsync(mutexKey, mutexToken, expiration, When.NotExists);

        return result ? mutexToken : string.Empty;
    }

    public async Task<bool> ReleaseMutexAsync(int showtimeId, string seatId, string mutexToken)
    {
        var mutexKey = GetMutexKey(showtimeId, seatId);
        return await _database.LockReleaseAsync(mutexKey, mutexToken);
    }

    public async Task<Domain.Entities.Seat> GetLockSeatAsync(int showtimeId, string seatId)
    {
        var lockKey = GetLockKey(showtimeId, seatId);
        var existingLock = await _database.StringGetAsync(lockKey);
        if (!existingLock.HasValue) return null;

        return JsonSerializer.Deserialize<Domain.Entities.Seat>(existingLock.ToString());
    }

    public async Task<bool> SetLockSeatAsync(int showtimeId, string seatId, string value, TimeSpan expiration)
    {
        var lockKey = GetLockKey(showtimeId, seatId);
        return await _database.StringSetAsync(lockKey, value, expiration, When.NotExists);
    }

    public async Task<bool> ReleaseLockAsync(int showtimeId, string seatId, string lockToken)
    {
        var lockKey = GetLockKey(showtimeId, seatId);

        var script = @"
            local value = redis.call('get', KEYS[1])
            if not value then
                return 0
            end

            if string.find(value, ARGV[1], 1, true) then
                return redis.call('del', KEYS[1])
            end

            return 0
        ";

        var result = await _database.ScriptEvaluateAsync(script, new RedisKey[] { lockKey }, new RedisValue[] { lockToken });
        return (int)result == 1;
    }
}
