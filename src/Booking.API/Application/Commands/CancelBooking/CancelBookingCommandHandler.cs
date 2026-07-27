namespace BookingService.API.Application.Commands.CancelBooking;

public class CancelBookingCommandHander : IRequestHandler<CancelBookingCommand, bool>
{
    private readonly IBookingRepository _bookingRepository;
    private readonly ILogger<CancelBookingCommandHander> _logger;
    private readonly IBookingIntegrationEventService _integrationEvent;

    public CancelBookingCommandHander(
        IBookingRepository bookingRepository, 
        ILogger<CancelBookingCommandHander> logger, 
        IBookingIntegrationEventService integrationEvent)
    {
        _bookingRepository = bookingRepository;
        _logger = logger;
        _integrationEvent = integrationEvent;
    }

    // Publish set status cancel and publish domain event
    public async Task<bool> Handle (CancelBookingCommand command, CancellationToken cancellationToken)
    {
        var bookingToUpdate = await _bookingRepository.GetByIdAsync(command.bookingId);
        if (bookingToUpdate == null)
        {
            return false;
        }

        bookingToUpdate.SetCancelledStatus();
        return await _bookingRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);
    }
}

// Đảm bảo chỉ 1 command tạo 1 domain event
public class CancelBookingIndentifiedCommandHandler : IdentifiedCommandHandler<CancelBookingCommand, bool>
{
    public CancelBookingIndentifiedCommandHandler(
        IMediator mediator,
        IRequestManager requestManager,
        ILogger<IdentifiedCommandHandler<CancelBookingCommand, bool>> logger) : base(mediator, requestManager, logger)
    {
    }

    protected override bool CreateResultForDuplicateRequest()
    {
        return true;
    }
}