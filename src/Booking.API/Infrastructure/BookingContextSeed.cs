
namespace BookingService.API.Infrastructure;

public class BookingContextSeed : IDbSeeder<BookingContext>
{
    public async Task SeedAsync(BookingContext context)
    {
        if (!context.CardTypes.Any())
        {
            context.CardTypes.AddRange(GetPredefinedCardTypes());
            await context.SaveChangesAsync();
        }
        await context.SaveChangesAsync();
    }

    private static IEnumerable<CardType> GetPredefinedCardTypes()
    {
        yield return new CardType (1, "Amex" );
        yield return new CardType (5, "Hehe" );

    }
}