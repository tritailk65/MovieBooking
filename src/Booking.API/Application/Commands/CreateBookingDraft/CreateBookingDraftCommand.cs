using BookingService.API.Application.Models;
using MediatR;

namespace BookingService.API.Application.Commands.CreateBookingDraft;

public record CreateBookingDraftCommand(string buyerId, IEnumerable<SeatItem> seats) : IRequest<BookingDraftDto>;