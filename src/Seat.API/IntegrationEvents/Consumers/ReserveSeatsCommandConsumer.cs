namespace Seat.API.IntegrationEvents.Consumer;

using MassTransit;
using SagaOrchestration.Contracts;
using Seat.API.Application.Seats.Command.ReserveSeat;

public sealed class ReserveSeatsCommandConsumer(
    IMediator mediator,
    ILogger<ReserveSeatsCommandConsumer> logger) : IConsumer<ReserveSeatsCommand>
{
    public async Task Consume(
        ConsumeContext<ReserveSeatsCommand> context)
    {
        var message = context.Message;

        logger.LogInformation(
            "Consuming ReserveSeatsCommand for reservation {ReservationId}",
            message.ReservationId);

        var result = await mediator.Send(
            new ReserveSeatApplicationCommand(
                message.ReservationId,
                message.BookingId,
                message.ShowtimeId,
                message.UserId,
                message.ReservationVersion),
            context.CancellationToken);

        if (!result.Succeeded)
        {
            await context.Publish(new SeatReservationFailedIntegrationEvent(message.ReservationId, message.BookingId, result.Reason));
            return;
        }

        await context.Publish(new SeatReservationHeldIntegrationEvent(message.ReservationId,message.BookingId));
    }
}