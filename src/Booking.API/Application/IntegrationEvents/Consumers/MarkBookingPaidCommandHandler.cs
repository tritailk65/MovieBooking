using MassTransit;
using SagaOrchestration.Contracts;

namespace BookingService.API.Application.IntegrationEvents.Consumers;

public sealed class MarkBookingPaidCommandConsumer(
    IMediator mediator,
    ILogger<MarkBookingPaidCommandConsumer> logger)
    : IConsumer<MarkBookingPaidCommand>
{
    public async Task Consume(ConsumeContext<MarkBookingPaidCommand> context)
    {
        var message = context.Message;

        logger.LogInformation(
            "Consuming MarkBookingPaidCommand for booking {BookingId}, reservation {ReservationId}",
            message.BookingId,
            message.ReservationId);

        var bookingMarkedAsPaid = await mediator.Send(
            new SetPaidBookingStatusCommand(message.BookingId),
            context.CancellationToken);

        if (!bookingMarkedAsPaid)
        {
            logger.LogWarning(
                "Could not mark booking {BookingId} as paid for reservation {ReservationId}",
                message.BookingId,
                message.ReservationId);

            // Let MassTransit retry and publish Fault<MarkBookingPaidCommand>.
            throw new InvalidOperationException(
                $"Could not mark booking {message.BookingId} as paid");
        }

        await context.Publish(
            new BookingStatusChangedToPaidIntegrationEvent(
                message.ReservationId,
                message.BookingId),
            context.CancellationToken);

        logger.LogInformation(
            "Marked booking {BookingId} as paid for reservation {ReservationId}",
            message.BookingId,
            message.ReservationId);
    }
}
