namespace BookingService.API.Application.Commands.CreateBooking;

public class CreateBookingCommandHandler : IRequestHandler<CreateBookingCommand, bool>
{
    private readonly IBookingRepository _bookingRepository;
    private readonly ILogger<CreateBookingCommandHandler> _logger;
    private readonly IBookingIntegrationEventService _integrationEvent;

    public CreateBookingCommandHandler(
        IBookingRepository bookingRepository, 
        ILogger<CreateBookingCommandHandler> logger, 
        IBookingIntegrationEventService integrationEvent)
    {
        _bookingRepository = bookingRepository;
        _logger = logger;
        _integrationEvent = integrationEvent;
    }

    public async Task<bool> Handle (CreateBookingCommand request, CancellationToken cancellationToken)
    {
        var booking = new Booking(request.UserId, request.UserName, request.ShowtimeId, request.ReservationId);

        foreach (var item in request.BookingItem)
        {
            booking.AddBookingItem(item.ShowtimeId, item.SeatId, item.BasePrice);
        }

        _bookingRepository.Add(booking);

        _logger.LogInformation(
            "Create booking for reservation {ReservationId}, showtime {ShowtimeId}, with {SeatCount} seats",
            request.ReservationId,
            request.ShowtimeId,
            request.BookingItem.Count());

        var saveBooking = await _bookingRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);
        if (saveBooking)
        {
            var bookingStartedIntegrationEvent = new BookingStartedIntegrationEvent(booking.Id, request.ReservationId, request.UserName, request.UserId);
            await _integrationEvent.AddAndSaveEventAsync(bookingStartedIntegrationEvent);

            _logger.LogInformation("Booking {BookingId} created for reservation {ReservationId}", booking.Id, request.ReservationId);
            return true;
        }
        else
        {
            _logger.LogError("Booking persistence failed for reservation {ReservationId}", request.ReservationId);
            return false;
        }

        
    }
}

public class CreateBookingIndentifiedCommandHandler : IdentifiedCommandHandler<CreateBookingCommand, bool>
{
    public CreateBookingIndentifiedCommandHandler(
        IMediator mediator,
        IRequestManager requestManager,
        ILogger<IdentifiedCommandHandler<CreateBookingCommand, bool>> logger) : base(mediator, requestManager, logger)
    {
    }

    protected override bool CreateResultForDuplicateRequest()
    {
        return true;
    }
}