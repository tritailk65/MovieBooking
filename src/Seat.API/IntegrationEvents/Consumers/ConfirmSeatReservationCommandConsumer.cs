
using MassTransit;
using SagaOrchestration.Contracts;
using Seat.API.Application.Command.ComfirmReservation;

public sealed class ConfirmSeatReservationCommandConsumer(
    IMediator mediator,
    ILogger<ConfirmSeatReservationCommandConsumer> logger)
    : IConsumer<ConfirmSeatReservationCommand>
{
    public async Task Consume(
        ConsumeContext<ConfirmSeatReservationCommand> context)
    {
        var message = context.Message;

        logger.LogInformation(
            "Consuming ConfirmSeatReservationCommand for reservation {ReservationId}",
            message.ReservationId);

        var result = await mediator.Send(
            new ConfirmSeatReservationApplicationCommand(
                message.ReservationId,
                message.BookingId,
                message.ShowtimeId,
                message.UserId,
                message.ReservationVersion),
            context.CancellationToken);

        if (!result.Succeeded)
        {
            logger.LogWarning(
                "Could not confirm seat reservation {ReservationId}: {Reason}",
                message.ReservationId,
                result.Reason);

            await context.Publish(
                new SeatReservationConfirmationFailedIntegrationEvent(
                    message.ReservationId,
                    message.BookingId,
                    result.Reason),
                context.CancellationToken);
            return;
        }

        await context.Publish(
            new SeatReservationConfirmedIntegrationEvent(
                message.ReservationId,
                message.BookingId),
            context.CancellationToken);

        logger.LogInformation(
            "Confirmed seat reservation {ReservationId}",
            message.ReservationId);
    }
}
