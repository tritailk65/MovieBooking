namespace SagaOrchestration;

public record BookingStatusChangedToCancelledIntegrationEvent : IntegrationEvent
{
    public int BookingId {get; }

    public string BookingStatus {get; }
    public string BuyerName {get;}
    
    public string BuyerId {get;}

    public BookingStatusChangedToCancelledIntegrationEvent(
        int bookingId,
        string bookingStatus,
        string buyerName,
        string buyerId)
    {
        BookingId = bookingId;
        BookingStatus = bookingStatus;
        BuyerName = buyerName;
        BuyerId = buyerId;
    }
}