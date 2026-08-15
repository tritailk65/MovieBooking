using BookingService.Domain.AggregateModel.BookingAggregates;
using MediatR;

namespace BookingService.API.Application.Commands.CancelBooking;

public record CancelBookingCommand(int bookingId) : IRequest<bool>;