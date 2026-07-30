namespace BookingService.API.Application.Commands.CreateBooking;

using SagaOrchestration.Contract;

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

        _logger.LogInformation("Create Booking - Booking: {@Booking}", booking);
        _bookingRepository.Add(booking);

        var saveBooking = await _bookingRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);
        if (saveBooking)
        {
            var bookingStartedIntegrationEvent = new BookingStartedIntegrationEvent(booking.Id, request.ReservationId, request.UserName, request.UserId);
            await _integrationEvent.AddAndSaveEventAsync(bookingStartedIntegrationEvent);
        }

        return true;
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