namespace BookingService.API.Application.Commands.SetAwaitingPayment;

/// <summary>
/// Command chuyển trạng thái booking sang payment để bắt đầu payment service
/// </summary>
public class SetAwaitingPaymentBookingStatusCommandHandler : IRequestHandler<SetAwaitingPaymentBookingStatusCommand, bool>
{
    private readonly IBookingRepository _bookingRepository;
    private readonly ILogger<SetAwaitingPaymentBookingStatusCommand> _logger;
    private readonly IBookingIntegrationEventService _integrationEvent;

    public SetAwaitingPaymentBookingStatusCommandHandler(
        IBookingRepository bookingRepository, 
        ILogger<SetAwaitingPaymentBookingStatusCommand> logger, 
        IBookingIntegrationEventService integrationEvent)
    {
        _bookingRepository = bookingRepository;
        _logger = logger;
        _integrationEvent = integrationEvent;
    }

    // Publish set status cancel and publish domain event
    public async Task<bool> Handle (SetAwaitingPaymentBookingStatusCommand command, CancellationToken cancellationToken)
    {
        var bookingToUpdate = await _bookingRepository.GetByIdAsync(command.bookingId);
        if (bookingToUpdate == null)
        {
            return false;
        }

        // TODO: Call to seat service again and check seat reservation 
        bookingToUpdate.SetAwaitingSeatValidationStatus();

        return await _bookingRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);
    }
}

// Đảm bảo chỉ 1 command tạo 1 domain event
public class SetAwaitingPaymentIndentifiedCommandHandler : IdentifiedCommandHandler<SetAwaitingPaymentBookingStatusCommand, bool>
{
    public SetAwaitingPaymentIndentifiedCommandHandler(
        IMediator mediator,
        IRequestManager requestManager,
        ILogger<IdentifiedCommandHandler<SetAwaitingPaymentBookingStatusCommand, bool>> logger) : base(mediator, requestManager, logger)
    {
    }

    protected override bool CreateResultForDuplicateRequest()
    {
        return true;
    }
}