namespace BookingService.API.Application.Commands.SetPaidBookingStatus;

public record SetPaidBookingStatusCommand(int bookingId) : IRequest<bool>;