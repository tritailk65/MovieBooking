
namespace BookingService.API;

public class BookingService(
    IMediator mediator,
    IBookingQueries bookingQueries,
    IIdentityService identityService,
    ILogger<BookingService> logger)
{
    public IMediator Mediator {get; set;} = mediator;
    public IBookingQueries BookingQueries {get;set;} = bookingQueries;
    public IIdentityService IdentityService {get; set;} = identityService;
    public ILogger<BookingService> Logger {get; set;} = logger;
}