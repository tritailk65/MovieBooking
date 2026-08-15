using MassTransit;
using SagaOrchestration.Contracts;
using Seat.API.Application.Seats.Command.ExtendSeatHold;

public sealed class ExtendSeatHoldCommandConsumer(
    IMediator mediator,
    ILogger<ExtendSeatHoldCommandConsumer> logger) : IConsumer<ExtendSeatHoldCommand>
{
    // Temporary value until the payment provider timeout is supplied by configuration/message contract.
    private static readonly TimeSpan PaymentProviderTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan SeatHoldSafetyBuffer = TimeSpan.FromMinutes(1);

    public async Task Consume(ConsumeContext<ExtendSeatHoldCommand> context)
    {   
        var message = context.Message;

        logger.LogInformation("Consuming ExtendSeatHoldCommand for reservation {ReservationId}", message.ReservationId);

        var result = await mediator.Send(
            new ExtendSeatHoldApplicationCommand(
                message.ReservationId,
                message.BookingId,
                message.ShowtimeId,
                message.UserId,
                PaymentProviderTimeout + SeatHoldSafetyBuffer),
            context.CancellationToken);

        if (!result.Succeeded)
        {
            await context.Publish(new SeatHoldExtensionFailedIntegrationEvent(message.ReservationId, message.BookingId, result.Reason));
            return;
        }
   
        await context.Publish(new SeatHoldExtendedIntegrationEvent(message.ReservationId, message.BookingId));
    }

}
