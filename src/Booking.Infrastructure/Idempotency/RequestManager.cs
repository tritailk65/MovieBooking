using BookingService.Domain.Exceptions;

namespace BookingService.Infrastructure.Idempotency;

public class RequestManager : IRequestManager
{
    private readonly BookingContext _context;

    public RequestManager(BookingContext context)
    {
        _context = context;
    }

    public async Task CreateRequestForCommandAsync<T>(Guid id)
    {
        var exist = await ExistAsync(id);

        var request = exist ? throw new BookingDomainException($"Request with {id} already exists"):
        new ClientRequest()
        {
            Id = id,
            Name = typeof(T).Name,
            Time = DateTime.UtcNow
        };
        _context.Add(request);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> ExistAsync(Guid id)
    {
        var request = await _context.FindAsync<ClientRequest>(id);
        return request != null;
    }
}