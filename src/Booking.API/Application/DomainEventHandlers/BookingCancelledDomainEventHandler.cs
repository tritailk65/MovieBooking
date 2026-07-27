namespace BookingService.API.Application.DomainEventHandlers;

public class BookingCancelledDomainEventHandler : INotificationHandler<BookingCancelledDomainEvent>
{
    private readonly IBookingRepository _bookingRepository;
    private readonly IBuyerRepository _buyerRepository;
    private readonly ILogger _logger;
    private readonly IBookingIntegrationEventService _integrationEvent;

    public BookingCancelledDomainEventHandler(
        IBookingRepository bookingRepo, 
        IBuyerRepository buyerRepo, 
        ILogger<BookingCancelledDomainEventHandler> logger, 
        IBookingIntegrationEventService bookingIntegrationEventService)
    {
        _bookingRepository = bookingRepo ?? throw new ArgumentNullException(nameof(bookingRepo));
        _buyerRepository = buyerRepo ?? throw new ArgumentNullException(nameof(buyerRepo));
        _logger = logger ?? throw new ArgumentException(nameof(logger));
        _integrationEvent = bookingIntegrationEventService ?? throw new ArgumentNullException(nameof(bookingIntegrationEventService));
    }

    public async Task Handle (BookingCancelledDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        BookingApiTrace.LogBookingStatusUpdated(_logger, domainEvent.Booking.Id, BookingStatus.Cancelled);

        var booking = await _bookingRepository.GetByIdAsync(domainEvent.Booking.Id);
        var buyer = await _buyerRepository.FindAsync(booking.UserId);
        
        var integrationEvent = new BookingStatusChangedToCancelledIntegrationEvent(booking.Id, booking.BookingStatus.Name, buyer.Name, buyer.IdentityGuid);
        
        // Mở transaction, ghi log và xử lý publish qua TransactionBehavior
        await _integrationEvent.AddAndSaveEventAsync(integrationEvent);
    }

}