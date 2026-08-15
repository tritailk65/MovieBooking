using BookingService.Domain.AggregateModel.BookingAggregates;
using MediatR;

namespace BookingService.Domain.Events;

// Domain event add buyer
public record class BookingStartedDomainEvent(
    Booking booking,
    string userName,
    string userId
) : INotification;