using BookingService.Domain.Events;
using BookingService.Domain.Exceptions;

namespace Booking.Domain.UnitTests.AggregateModel.BookingAggregates;

public class BookingTests
{

    [Fact]
    public void Constructor_WhenValidInput_ShouldCreateSubmittedBooking()
    {
        var reservationId = Guid.NewGuid();

        var booking = new BookingService.Domain.AggregateModel.BookingAggregates.Booking(
            userId: "user-1",
            userName: "Test User",
            showtimeId: 10,
            reservationId: reservationId
        );

        Assert.Equal("user-1", booking.UserId);
        Assert.Equal(10, booking.ShowtimeId);
        Assert.Equal(reservationId, booking.ReservationId);
        Assert.NotEqual(default, booking.BookingAt);
        Assert.Contains(booking.DomainEvents, e => e is BookingStartedDomainEvent);
        Assert.Contains(booking.DomainEvents, e => e is BookingCreatedDomainEvent);
    }

    [Fact]
    public async Task AddBookingItem_WhenSeatAlreadyExists_ShouldThrowBookingDomainException()
    {
        var booking = CreateBooking();

        booking.AddBookingItem(10, "A1", 90000m);

        Assert.Throws<BookingDomainException>(() =>
            booking.AddBookingItem(10, "A1", 90000m));
    }

    [Fact]
    public void GetTotal_WhenBookingHasItems_ShouldReturnSumOfBasePrices()
    {
        var booking = CreateBooking();

        booking.AddBookingItem(10, "A1", 90000m);
        booking.AddBookingItem(10, "A2", 110000m);

        Assert.Equal(200000m, booking.GetTotal());
    }

    private static BookingService.Domain.AggregateModel.BookingAggregates.Booking CreateBooking()
    {
        return new BookingService.Domain.AggregateModel.BookingAggregates.Booking(
            userId: "user-1",
            userName: "Test User",
            showtimeId: 10,
            reservationId: Guid.NewGuid()
        );
    }


    [Fact]
    public void SetPaidStatus_WhenBookingIsSeatConfirmed_ShouldAddBookingPaidDomainEvent()
    {
        var booking = CreateBooking();

        booking.SetSeatConfirmedStatus();
        booking.SetPaidStatus();

        Assert.Contains(booking.DomainEvents, e => e is BookingPaidDomainEvent);
    }
}