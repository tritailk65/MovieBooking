namespace PaymentService.Api;

public record BookingPaymentSucceededIntegrationEvent : IntegrationEvent
{
    public int BookingId {get; }
    public int ShowtimeId {get;}
    public string UserId {get;}
    public string ReservationId {get;}

    public BookingPaymentSucceededIntegrationEvent(int bookingId, int showtimeId, string userId, string reservationId)
    {
        BookingId = bookingId;
        ShowtimeId = showtimeId;
        UserId = userId;
        ReservationId = reservationId;
    }
}