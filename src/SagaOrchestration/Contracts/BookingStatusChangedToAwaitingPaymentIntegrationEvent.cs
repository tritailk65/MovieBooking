namespace SagaOrchestration.Contracts;

public record BookingStatusChangedToAwaitingPaymentIntegrationEvent : IntegrationEvent
{
    public int BookingId {get; }
    public string BookingStatus {get; }
    public string BuyerName {get;}
    public string BuyerId {get;}
    public int ShowtimeId {get;}
    public string ReservationId {get;}

    public BookingStatusChangedToAwaitingPaymentIntegrationEvent (
        int bookingId,
        string bookingStatus,
        string buyerName,
        string buyerId,
        int showtimeId,
        string reservationId)
    {
        BookingId = bookingId;
        BookingStatus = bookingStatus;
        BuyerName = buyerName;
        BuyerId = buyerId;
        ShowtimeId = showtimeId;
        ReservationId = reservationId;
    }
}
