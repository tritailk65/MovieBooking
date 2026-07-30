using Grpc.Core;
using Microsoft.AspNetCore.Authorization;

namespace Seat.API.Grpc;

public class SeatService(
    IMediator mediator,
    ILogger<SeatService> logger) : SeatGrpc.SeatGrpcBase
{
    //[Authorize(Policy = "Permission:seat.write")]
    public override async Task<ValidationReservationResponse> ValidationReservation(ValidationReservationRequest request, ServerCallContext context)
    {
        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogDebug("Begin ValidationReservation call from method {Method} for reservation id {Id}", context.Method, request.ReservationId);
        }

        // TODO: Booking Service call this to make sure the list of seat is available and can checkout, so:
        // - Check ReservationId, UserId
        // - Check lock, userlock, reamainingSecond, calculate price, change status from Active to PreparedForPayment
        // - Increase remainingSecond if it not enough for payment phase
        // - Return Immutable snapshot for Saga Begin


        var command = new ValidationReservationCommand(request.ShowtimeId, request.ReservationId, request.UserId);
        var result = await mediator.Send(command);

        if (result is null)
        {
            return new ValidationReservationResponse { Success = false };
        }

        var response = new ValidationReservationResponse
        {
            Success = true,
            ReservationId = result.Id.ToString(),
            ShowtimeId = result.ShowtimeId,
            UserId = result.UserId,
            RemainingSeconds = result.RemainingSeconds,
            BasePrice = decimal.ToDouble(result.BasePrice)
        };

        response.SeatIds.AddRange(result.SeatIds);

        return response;
    }

    // TODO: Create new Function for get snapshot Seats for draft (by UserId)

    //[Authorize(Policy = "Permission:seat.write")]
    public override async Task<ReleaseSeatReservationResponse> ReleaseSeatReservation(ReleaseSeatReservationRequest request, ServerCallContext context)
    {

        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogDebug("Begin ReleaseSeatReservation call from method {Method} for basket id {Id}", context.Method, request.UserId);
        }

        var command = new ReleaseSeatReservationCommand(request.ShowtimeId, request.ReservationId, request.UserId);
        var result = await mediator.Send(command);

        var response = new ReleaseSeatReservationResponse{ Success = result};
        return response;
    }
}
