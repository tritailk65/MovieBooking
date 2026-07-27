
Luồng đúng trong code là:

```text
1. CreateOrderCommand
   -> CreateOrderCommandHandler
   -> tạo Order
   -> lưu DB
   -> phát OrderStartedIntegrationEvent

2. Catalog confirm stock
   -> phát OrderStockConfirmedIntegrationEvent

3. Ordering nhận stock confirmed
   -> chuyển order sang trạng thái phù hợp, thường là Paid/awaiting payment tùy flow
   -> payment process xử lý

4. Payment service báo lỗi
   -> phát OrderPaymentFailedIntegrationEvent qua event bus

5. Ordering lắng nghe event bus bằng:
   OrderPaymentFailedIntegrationEventHandler
```

Nói ngắn gọn, flow chuẩn thanh toán là:

```text
PaymentFailedIntegrationEvent
-> OrderPaymentFailedIntegrationEventHandler
-> mediator.Send(CancelOrderCommand)
-> CancelOrderCommandHandler
-> Order.SetCancelledStatus()
-> OrderCancelledDomainEvent
-> OrderCancelledDomainEventHandler
-> OrderStatusChangedToCancelledIntegrationEvent
-> publish ra event bus sau commit
```

Nói ngắn gọn:
```text
Nếu logic thuộc về 1 aggregate -> method trong aggregate.
Nếu logic cần nhiều aggregate và là luật nghiệp vụ -> Domain Service.
Nếu logic là load/save/gọi API/transaction -> CommandHandler hoặc Application Service.
```