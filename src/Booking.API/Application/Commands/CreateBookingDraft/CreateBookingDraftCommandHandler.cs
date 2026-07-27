using BookingService.API.Application.Models;
using BookingService.Domain.AggregateModel.BookingAggregates;
using MediatR;

namespace BookingService.API.Application.Commands.CreateBookingDraft;

public class CreateBookingDraftCommandHandler : IRequestHandler<CreateBookingDraftCommand, BookingDraftDto>
{
    public Task<BookingDraftDto> Handle(CreateBookingDraftCommand message, CancellationToken cancellationToken)
    {
        var booking = Booking.NewDraft();

        foreach (var item in message.seats)
        {
            //order.AddOrderItem(item.ProductId, item.ProductName, item.UnitPrice, item.Discount, item.PictureUrl, item.Units);
            booking.AddBookingItem(item.ShowtimeId, item.SeatId, item.BasePrice);
        }
        return Task.FromResult(BookingDraftDto.FromBooking(booking));
    }

}
    public record BookingDraftDto
    {
        public IEnumerable<SeatItem> Seats {get; init;}
        public decimal Total {get; init;}
        public static BookingDraftDto FromBooking(Booking booking)
        {
            return new BookingDraftDto()
            {
                Seats = booking.BookingItems.Select(bi => new SeatItem
                {
                    
                    ShowtimeId = bi.Showtime,
                    SeatId = bi.SeatId,
                    BasePrice = bi.BasePrice
                }),
                Total = booking.GetTotal()
            };
        }
    } 