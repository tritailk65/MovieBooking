namespace SagaOrchestration;

public record BookingPaymentFailedIntegrationEvent : IntegrationEvent
{
    public int BookingId {get; }
    public int ShowtimeId {get;}
    public string UserId {get;}
    public Guid ReservationId {get;}

    public BookingPaymentFailedIntegrationEvent(int bookingId, int showtimeId, string userId, Guid reservationId)
    {
        BookingId = bookingId;
        ShowtimeId = showtimeId;
        UserId = userId;
        ReservationId = reservationId;
    }
}