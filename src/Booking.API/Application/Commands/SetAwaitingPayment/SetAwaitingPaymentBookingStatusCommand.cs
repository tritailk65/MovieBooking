namespace BookingService.API.Application.Commands.SetAwaitingPayment;

public record SetAwaitingPaymentBookingStatusCommand(int bookingId) : IRequest<bool>;