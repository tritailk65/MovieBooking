// using Seat.API.Application.Command.ComfirmReservation;

// namespace Seat.API.IntegrationEvents.EventHandlers;

// public class BookingPaymentSucceededIntegrationEventHandler : IIntegrationEventHandler<BookingPaymentSucceededIntegrationEvent>
// {
//     private readonly ILogger<BookingPaymentSucceededIntegrationEvent> _logger;
//     private readonly IMediator _mediator;

//     public BookingPaymentSucceededIntegrationEventHandler(
//         IMediator mediator,
//         ILogger<BookingPaymentSucceededIntegrationEvent> logger)
//     {
//         _logger = logger;
//         _mediator = mediator;
//     }

//     public async Task Handle(BookingPaymentSucceededIntegrationEvent @event)
//     {
//         _logger.LogInformation("Received PaymentSucceededIntegrationEvent: {BookingId}",
//             @event.BookingId);

//         var confirmReservationCommand = new ConfirmSeatReservationApplicationCommand(@event.ShowtimeId, @event.ReservationId, @event.UserId);

//         var result = await _mediator.Send(confirmReservationCommand);

        
//     }
// }