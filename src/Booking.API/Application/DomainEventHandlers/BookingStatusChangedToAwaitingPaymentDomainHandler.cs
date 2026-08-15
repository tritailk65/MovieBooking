using SagaOrchestration.Contracts;

namespace BookingService.API.Application.DomainEventHandlers;

public class BookingStatusChangeToAwaitingPaymentDomainEventHandler : INotificationHandler<BookingAwaitingPaymentDomainEvent>
{
    private readonly IBookingRepository _bookingRepository;
    private readonly IBuyerRepository _buyerRepository;
    private readonly ILogger _logger;
    private readonly IBookingIntegrationEventService _integrationEvent;

    public BookingStatusChangeToAwaitingPaymentDomainEventHandler(
        IBookingRepository bookingRepo, 
        IBuyerRepository buyerRepo, 
        ILogger<BookingAwaitingPaymentDomainEvent> logger, 
        IBookingIntegrationEventService bookingIntegrationEventService)
    {
        _bookingRepository = bookingRepo ?? throw new ArgumentNullException(nameof(bookingRepo));
        _buyerRepository = buyerRepo ?? throw new ArgumentNullException(nameof(buyerRepo));
        _logger = logger ?? throw new ArgumentException(nameof(logger));
        _integrationEvent = bookingIntegrationEventService ?? throw new ArgumentNullException(nameof(bookingIntegrationEventService));
    }

    public async Task Handle (BookingAwaitingPaymentDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        BookingApiTrace.LogBookingStatusUpdated(_logger, domainEvent.Booking.Id, BookingStatus.AwaitingSeatValidation);

        var booking = await _bookingRepository.GetByIdAsync(domainEvent.Booking.Id);
        var buyer = await _buyerRepository.FindAsync(booking.UserId);
        
        // var integrationEvent = new BookingStatusChangedToAwaitingPaymentIntegrationEvent(
        //     booking.Id, 
        //     booking.BookingStatus.Name, 
        //     buyer.Name, 
        //     buyer.IdentityGuid,
        //     booking.ShowtimeId,
        //     booking.ReservationId.ToString()
        // );
        
        // // Mở transaction, ghi log và xử lý publish qua TransactionBehavior
        // await _integrationEvent.AddAndSaveEventAsync(integrationEvent);

        var paymentRequest = new PaymentRequestedIntegrationEvent(booking.ReservationId, booking.Id, booking.UserId, booking.GetTotal());

        await _integrationEvent.AddAndSaveEventAsync(paymentRequest);

    }

}
