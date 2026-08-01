namespace BookingService.API.Application.IntegrationEvents.Events;

public record  BookingStartedIntegrationEvent : IntegrationEvent
{
    public int BookingId {get; set;}
    public Guid ReservationId {get; set;}
    public string BuyerId {get; set;}
    public string BuyerName {get; set;}

    public BookingStartedIntegrationEvent(
        int bookingId,
        Guid reservationId,
        string buyerName,
        string buyerId)
    {
        BookingId = bookingId;
        ReservationId = reservationId;
        BuyerName = buyerName;
        BuyerId = buyerId;
    }
}