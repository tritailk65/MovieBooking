using SagaOrchestration.Contracts;

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

        var integrationEvent =
            new BookingStatusChangedToSubmittedIntegrationEvent(
                ReservationId: domainEvent.booking.ReservationId,
                BookingId: domainEvent.booking.Id,
                ShowtimeId: domainEvent.booking.ShowtimeId,
                UserId: domainEvent.userId,
                SeatIds: domainEvent.booking.BookingItems
                    .Select(x => x.SeatId)
                    .ToArray(),
                TotalPrice: domainEvent.booking.GetTotal(),
                ReservationVersion: domainEvent.booking.ReservationVersion,
                PreparedUntil: DateTime.Now.AddMinutes(10));

        await _integrationEvent.AddAndSaveEventAsync(integrationEvent);
        BookingApiTrace.LogBookingBuyerAndPaymentValidatedOrUpdated(_logger, buyer.Id, domainEvent.booking.Id);
    }

}
