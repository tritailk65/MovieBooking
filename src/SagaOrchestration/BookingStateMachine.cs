
public sealed class BookingStateMachine : MassTransitStateMachine<BookingSaga>
{
    private readonly ILogger<BookingStateMachine> _logger;

    public State BookingCreated { get; private set; } = null!;
    public State BookingCanceled {get; private set; } = null!;
    public State BookingWaitingForPayment {get; private set; } = null!;
    public State Failed {get; private set;} = null!;

    private Event<BookingStartedIntegrationEvent> OnSagaStared {get; set; } = null!;
    private Event<BookingPaymentSucceededIntegrationEvent> OnPaymentSuccess {get; set; } = null!;
    private Event<BookingPaymentFailedIntegrationEvent> OnPaymentFailed {get; set;} = null!;
    private Event<BookingStatusChangedToCancelledIntegrationEvent> OnBookingCancled {get; set;} = null!;
    private Event<BookingStatusChangedToSubmittedIntegrationEvent> OnBookingSubmited {get; set;} = null!;

    public BookingStateMachine(ILogger<BookingStateMachine> logger)
    {
        _logger = logger;

        InstanceState(x => x.CurrentState);

        // Event(() => OnSagaStared, context => context.CorrelateById(x => x.Message.));
    }

    // public OrderStateMachine(ILogger<OrderStateMachine> logger)
    // {
    //     _logger = logger;

    //     InstanceState(x => x.CurrentState);

    //     Event(() => OnSagaStarted, context => context.CorrelateById(x => x.Message.OrderId));
    //     Event(() => OnGoodsBookedInWarehouseSuccess, context => context.CorrelateById(x => x.Message.OrderId));
    //     Event(() => OnGoodsBookedInWarehouseFailed, context => context.CorrelateById(x => x.Message.OrderId));
    //     Event(() => OnOrderCreateSuccess, context => context.CorrelateById(x => x.Message.OrderId));
    //     Event(() => OnOrderCreateFailed, context => context.CorrelateById(x => x.Message.OrderId));
    //     Event(() => OnDeliverySendEventSuccess, context => context.CorrelateById(x => x.Message.OrderId));
    //     Event(() => OnDeliverySendEventFailed, context => context.CorrelateById(x => x.Message.OrderId));
    //     Event(() => OnGoodsBookedRejected, context => context.CorrelateById(x => x.Message.OrderId));

    //     // Map correlation for fault state
    //     Event(() => OnCancelOrderFault, x => x.CorrelateById(context => context.Message.Message.OrderId));
    //     Event(() => OnRejectOrderFault, x => x.CorrelateById(context => context.Message.Message.OrderId));
    //     Event(() => OnRestoreFault, x => x.CorrelateById(context => context.Message.Message.OrderId));
        

    //     Initially(WhenSagaStarted());

    //     During(OrderCreated,
    //         When(OnOrderCreateSuccess)
    //             .Then(LogSagaState)
    //             .Publish(context => new InventoryGoodsBookedInWarehouseEvent(context.Message.OrderId, context.Message.UserId, context.Message.CartItems, context.Message.Address))                
    //             .TransitionTo(BookingGoodsInWarehouse),
    //         When(OnOrderCreateFailed)
    //             .Publish(context => new OrderCancelEvent(context.Message.OrderId))
    //             .TransitionTo(Canceled)
    //     );

    //     During(BookingGoodsInWarehouse,
    //         When(OnGoodsBookedInWarehouseSuccess)
    //             .Then(LogSagaState)
    //             .Publish(context => new DeliverySendEvent(context.Message.OrderId, context.Message.CartItems, context.Message.UserId, context.Message.Address))
    //             .TransitionTo(DeliverySend),
    //         When(OnGoodsBookedRejected)
    //             .Then(LogSagaState)
    //             .TransitionTo(Rejected),
    //         When(OnGoodsBookedInWarehouseFailed)
    //             .Then(LogSagaState)
    //             .Publish(context => new OrderCancelEvent(context.Message.OrderId))
    //             .TransitionTo(Canceled)    
    //     );

    //     During(DeliverySend,
    //         When(OnDeliverySendEventSuccess)
    //             .Then(LogSagaState)
    //             .TransitionTo(Final),
    //         When(OnDeliverySendEventFailed)
    //             .Then(LogSagaState)
    //             .Publish(context => new InventoryGoodsRestoredEvent(context.CorrelationId!.Value, context.Saga.Goods.ToDictionary(id => id.Id, model => model.Count)))
    //             .Publish(context => new OrderCancelEvent(context.Message.OrderId))
    //             .TransitionTo(Canceled)
    //     );

    //     During(Rejected, Canceled,
    //         When(OnRejectOrderFault)
    //             .Then(LogSagaState)
    //             .TransitionTo(Failed),
    //         When(OnCancelOrderFault)
    //             .Then(LogSagaState)
    //             .TransitionTo(Failed),
    //         When(OnRestoreFault)
    //             .Then(LogSagaState)
    //             .TransitionTo(Failed)
    //     );
        
    // }

    // private EventActivityBinder<OrderSaga, OrderCreateEvent> WhenSagaStarted()
    // {
    //     return When(OnSagaStarted)
    //         .Then(InitializeSaga)
    //         .TransitionTo(OrderCreated);
    // }

    // private void InitializeSaga(BehaviorContext<OrderSaga, OrderCreateEvent> context)
    // {
    //     context.Saga.Goods = context.Message.CartItems;
    //     context.Saga.DeliveryAddress = context.Message.Address;
    //     context.Saga.CorrelationId = context.Message.OrderId;
    //     context.Saga.UserId = context.Message.UserId;
    //     context.Saga.RequestId = context.RequestId;
    //     context.Saga.ResponseAddress = context.ResponseAddress;
    //     context.Saga.CreatedAt = DateTime.UtcNow;
    // }

    // private void LogSagaState<TEvent>(BehaviorContext<OrderSaga, TEvent> context) where TEvent : class
    // {
    //     _logger.LogCritical($"{nameof(OrderSaga)} | correlationId: {context.Saga.CorrelationId} | event: {context.Event.Name}");
    //     context.Saga.UpdateAt = DateTime.UtcNow;
    // }

}
