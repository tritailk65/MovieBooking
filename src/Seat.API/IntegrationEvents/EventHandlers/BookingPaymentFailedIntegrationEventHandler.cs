
namespace Seat.API.IntegrationEvents.EventHandlers;

public class BookingPaymentFailedIntegrationEventHandler : IIntegrationEventHandler<BookingPaymentFailedIntegrationEvent>
{
    private readonly ILogger<BookingPaymentFailedIntegrationEvent> _logger;
    private readonly IMediator _mediator;

    public BookingPaymentFailedIntegrationEventHandler(
        IMediator mediator,
        ILogger<BookingPaymentFailedIntegrationEvent> logger)
    {
        _logger = logger;
        _mediator = mediator;
    }

    public async Task Handle(BookingPaymentFailedIntegrationEvent @event)
    {
        _logger.LogInformation("Received PaymentFailedIntegrationEvent: {BookingId}",
            @event.BookingId);

        var cmd = new ReleaseSeatReservationCommand(@event.ShowtimeId, @event.ReservationId, @event.UserId);

        var result = await _mediator.Send(cmd);

        
    }
}