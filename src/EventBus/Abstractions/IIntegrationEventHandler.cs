namespace EventBus.Abstractions;

/// <summary>
/// Interface khi nhận message thì xử lý cái gì
/// </summary>
/// <typeparam name="TIntegrationEvent"></typeparam>
public interface IIntegrationEventHandler<in TIntegrationEvent> : IIntegrationEventHandler
    where TIntegrationEvent : IntegrationEvent
{
    Task Handle(TIntegrationEvent @event);

    Task IIntegrationEventHandler.Handle(IntegrationEvent @event) => Handle((TIntegrationEvent)@event);
}

/// <summary>
/// Interface gửi đi mesage
/// </summary>
public interface IIntegrationEventHandler
{
    Task Handle(IntegrationEvent @event);
}