namespace SagaOrchestration;

public record BookingPaymentSucceededIntegrationEvent : IntegrationEvent
{
    public int BookingId {get; }
    public int ShowtimeId {get;}
    public string UserId {get;}
    public Guid ReservationId {get;}

    public BookingPaymentSucceededIntegrationEvent(int bookingId, int showtimeId, string userId, Guid reservationId)
    {
        BookingId = bookingId;
        ShowtimeId = showtimeId;
        UserId = userId;
        ReservationId = reservationId;
    }
}