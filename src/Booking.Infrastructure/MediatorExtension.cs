namespace BookingService.Infrastructure;

static class MediatorExtension
{
    public static async Task DispatchDomainEventsAsync(this IMediator mediator, BookingContext ctx)
    {
        // Tìm tất cả các entity đang có Domain event chưa được phát
        var domainEntities = ctx.ChangeTracker
            .Entries<Entity>()
            .Where(x => x.Entity.DomainEvents != null && x.Entity.DomainEvents.Any());

        // Thu các event gom lại thành list
        var domainEvents = domainEntities
            .SelectMany(x => x.Entity.DomainEvents)
            .ToList();

        //Xoá các event khỏi entity để tránh phát lại
        domainEntities.ToList()
            .ForEach(entity => entity.Entity.ClearDomainEvents());

        // publísh từng event
        foreach (var domainEvent in domainEvents)
            await mediator.Publish(domainEvent);
    }
}