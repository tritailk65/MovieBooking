namespace SagaOrchestration;

public record BookingStatusChangedToSubmittedIntegrationEvent : IntegrationEvent
{
    public int BookingId {get; }
    public string BookingStatus {get; }
    public string BuyerName {get;}
    public Guid ReservationId {get;}
    public string BuyerId {get;}

    public BookingStatusChangedToSubmittedIntegrationEvent(
        int bookingId,
        string bookingStatus,
        string buyerName,
        Guid reservationId,
        string buyerId)
    {
        BookingId = bookingId;
        BookingStatus = bookingStatus;
        BuyerName = buyerName;
        ReservationId = reservationId;
        BuyerId = buyerId;
    }
}
