using EventBus.Events;

namespace Seat.API.IntegrationEvents.Events;

public record BookingPaymentFailedIntegrationEvent : IntegrationEvent
{
    public int BookingId {get; }
    public int ShowtimeId {get;}
    public string UserId {get;}
    public string ReservationId {get;}

    public BookingPaymentFailedIntegrationEvent(int bookingId, int showtimeId, string userId, string reservationId)
    {
        BookingId = bookingId;
        ShowtimeId = showtimeId;
        UserId = userId;
        ReservationId = reservationId;
    }
}