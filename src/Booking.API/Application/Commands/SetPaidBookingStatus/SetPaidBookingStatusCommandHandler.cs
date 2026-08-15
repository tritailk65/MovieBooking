namespace BookingService.API.Application.Commands.SetPaidBookingStatus;

/// <summary>
/// Command chuyển trạng thái booking sang payment để bắt đầu payment service
/// </summary>
public class SetPaidBookingStatusCommandHandler : IRequestHandler<SetPaidBookingStatusCommand, bool>
{
    private readonly IBookingRepository _bookingRepository;

    public SetPaidBookingStatusCommandHandler(
        IBookingRepository bookingRepository, 
        ILogger<SetPaidBookingStatusCommand> logger, 
        IBookingIntegrationEventService integrationEvent)
    {
        _bookingRepository = bookingRepository;
    }

    // Publish set status cancel and publish domain event
    public async Task<bool> Handle (SetPaidBookingStatusCommand command, CancellationToken cancellationToken)
    {
        var bookingToUpdate = await _bookingRepository.GetByIdAsync(command.bookingId);
        if (bookingToUpdate == null)
        {
            return false;
        }
 
        bookingToUpdate.SetPaidStatus();

        return await _bookingRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);
    }
}

// Đảm bảo chỉ 1 command tạo 1 domain event
public class SetPaidBookingStatusIndentifiedCommandHandler : IdentifiedCommandHandler<SetPaidBookingStatusCommand, bool>
{
    public SetPaidBookingStatusIndentifiedCommandHandler(
        IMediator mediator,
        IRequestManager requestManager,
        ILogger<IdentifiedCommandHandler<SetPaidBookingStatusCommand, bool>> logger) : base(mediator, requestManager, logger)
    {
    }

    protected override bool CreateResultForDuplicateRequest()
    {
        return true;
    }
}