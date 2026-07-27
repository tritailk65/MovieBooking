namespace BookingService.API.Application.IntegrationEvents.Events;

public record  BookingStartedIntegrationEvent : IntegrationEvent
{
    public string UserId {get; set;}

    public BookingStartedIntegrationEvent(string userId) => UserId = userId;
}