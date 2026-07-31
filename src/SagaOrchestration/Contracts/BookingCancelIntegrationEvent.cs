namespace SagaOrchestration.Contracts;

public record BookingCancelIntegrationEvent : IntegrationEvent
{
    public int BookingId {get; set;}
    public int ShowtimeId {get; set;}
    public string UserId {get; set;}
    public Guid ReservationId {get; set;}

    public BookingCancelIntegrationEvent(int bookingId, int showtimeId, string userId, Guid reservationId)
    {
        BookingId = bookingId;
        ShowtimeId = showtimeId;
        UserId = userId;
        ReservationId = reservationId;
    }
}