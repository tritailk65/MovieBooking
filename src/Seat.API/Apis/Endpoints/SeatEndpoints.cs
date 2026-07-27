using Seat.API.Application.Seats.GetSeatReservation;

namespace Seat.API.Endpoints;

public static class SeatEndpoints
{
    public static IEndpointRouteBuilder MapSeatApi(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/seat").WithTags("Seat API");

        #region Endpoint read
        //Get seat by showtime id
        group.MapGet("/{showtimeId:int}/map", async (int showtimeId, IMediator mediator) =>
        {
            var query = new GetShowtimeSeatQuery(showtimeId);
            var seats = await mediator.Send(query);
            
            return Results.Ok(seats);
        })
        .WithName("GetShowtimeSeats")
        .Produces<IEnumerable<SeatDto>>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest);

        group.MapGet("/reservation", async (int showtimeId, string userId, IMediator mediator) =>
        {
            var query = new GetSeatReservationQuery(showtimeId, userId);
            var reservation = await mediator.Send(query);
            
            return Results.Ok(reservation);
        })
        .WithName("GetSeatReservation")
        .Produces<IEnumerable<SeatReservation>>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest);
        #endregion

        #region Endpoint write
        //Lock seat
        group.MapPost("/lock", async (LockSeatCommand command, IMediator mediator) =>
        {
            var result = await mediator.Send(command);

            if (result.IsSuccess)
            {
                return Results.Ok(new
                {
                    Message = "Locked seat successfully!",
                    result.LockToken,
                    result.LockExpiration
                });
            }
            
            // Trả về 409 Conflict tranh chấp tài nguyên thất bại
            return Results.Conflict(new { Message = "This seat have been ordered" });
        });

        // THis endpoint use for testing, in acctual this command should be called in IntegraionEvent Handling
        group.MapPost("/release", async (ReleaseSeatCommand command, IMediator mediator) =>
        {
            var isSuccess = await mediator.Send(command);

            if (isSuccess)
            {
                return Results.Ok(new {Message = "Released seat successfully"});
            }
            return Results.Conflict(new {Mesage = "Release seat fail, seat already have been booking or ordered"});
       });

        group.MapPost("/markseatsold", async (MarkSeatSoldCommand command, IMediator mediator) =>
        {
            var isSuccess = await mediator.Send(command);

            if (isSuccess)
            {
                return Results.Ok(new {Message = "Mark seat sold successfully"});
            }
            return Results.Conflict(new {Mesage = "Mark seat sold fail, seat already have been booking or ordered"});
        });

        // Endpoint release reservation khi user quyết định thanh toán
        // Đặt trước tên endpoint là "confirm"
        group.MapPost("/reservation-release", async (ReleaseSeatReservationCommand command, IMediator mediator) =>
        {
            var isSuccess = await mediator.Send(command);

            if (isSuccess)
            {
                return Results.Ok(new {Message = "Release reservation successfully"});
            }
            return Results.Conflict(new {Mesage = "Release seat reservation fail"});
        });
        
        // Validation và bắt đầu chuyển qua booking service
        group.MapPut("/validation-reservation", async (ValidationReservationCommand command, IMediator mediator) =>
        {
            var result = await mediator.Send(command);

            if (result is not null)
            {
                return Results.Ok(result);
            }
            return Results.Conflict(new {Mesage = "Validation seat reservation fail"});
        });
        
        // KHi user payment thanh toan thanh cong
        group.MapPost("/confirm-reservation", async (ConfirmReservationCommand command, IMediator mediator) => {
            var result = await mediator.Send(command);

            if (result is not null)
            {
                return Results.Ok(result);
            }
            return Results.Conflict(new {Mesage = "Confirm seat reservation fail"});
        });

        

       #endregion
        
        return group;
    }
}
