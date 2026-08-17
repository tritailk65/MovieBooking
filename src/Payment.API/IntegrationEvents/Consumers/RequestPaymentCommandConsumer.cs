namespace PaymentService.Api.IntegrationEvents.Consumers;

using MassTransit;
using Microsoft.Extensions.Options;
using SagaOrchestration.Contracts;

public sealed class RequestPaymentCommandConsumer(
    IOptionsMonitor<PaymentOptions> options,
    ILogger<RequestPaymentCommandConsumer> logger)
    : IConsumer<RequestPaymentCommand>
{
    public async Task Consume(
        ConsumeContext<RequestPaymentCommand> context)
    {
        var message = context.Message;

        logger.LogInformation(
            "Consuming RequestPaymentCommand for reservation {ReservationId}",
            message.ReservationId);

        if (options.CurrentValue.PaymentSucceeded)
        {
            await Task.Delay(TimeSpan.FromSeconds(10), context.CancellationToken);

            await context.Publish(new PaymentSucceededIntegrationEvent(message.ReservationId, message.BookingId,
                    Guid.NewGuid().ToString()),
                context.CancellationToken);

            return;
        }

        await Task.Delay(
            TimeSpan.FromSeconds(3),
            context.CancellationToken);

        await context.Publish(
            new PaymentFailedIntegrationEvent(
                message.ReservationId,
                message.BookingId,
                "Failed to process payment"),
            context.CancellationToken);
    }
}