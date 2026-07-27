namespace BookingService.API.Application.DomainEventHandlers;

public class ValidateOrAddBuyerDomainEventHandler : INotificationHandler<BookingStartedDomainEvent>
{
    private readonly ILogger _logger;
    private readonly IBuyerRepository _buyerRepository;
    private readonly IBookingIntegrationEventService _integrationEvent;
    
    public ValidateOrAddBuyerDomainEventHandler(
        IBookingIntegrationEventService integrationEvent, 
        IBuyerRepository buyerRepo, 
        ILogger<ValidateOrAddBuyerDomainEventHandler> logger)
    {
        _buyerRepository = buyerRepo ?? throw new ArgumentNullException(nameof(buyerRepo));
        _logger = logger ?? throw new ArgumentException(nameof(logger));
        _integrationEvent = integrationEvent ?? throw new ArgumentNullException(nameof(integrationEvent));
    }

    public async Task Handle (BookingStartedDomainEvent domainEvent, CancellationToken cancellationToken)
    {

        //var cardTypeId = domainEvent.CardTypeId != 0 ? domainEvent.CardTypeId : 1;
        var buyer = await _buyerRepository.FindAsync(domainEvent.userId);
        var buyerExisted = buyer is not null;

        if (!buyerExisted)
        {
            buyer = new Buyer(domainEvent.userId, domainEvent.userName);
        }

        // TODO: Add Payment method or get exist payment method

        if (!buyerExisted)
        {
            _buyerRepository.Add(buyer);
        }

        await _buyerRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        // Event confirm payment method and buyer created or exist 
        var integrationEvent = new BookingStatusChangedToSubmittedIntegrationEvent(
            domainEvent.booking.Id, 
            BookingStatus.Submitted.Name, 
            domainEvent.userName, 
            domainEvent.userId);

        await _integrationEvent.AddAndSaveEventAsync(integrationEvent);
        BookingApiTrace.LogBookingBuyerAndPaymentValidatedOrUpdated(_logger, buyer.Id, domainEvent.booking.Id);
    }

}
