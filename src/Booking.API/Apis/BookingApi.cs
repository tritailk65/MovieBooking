using ServiceDefaults.Authorization;

namespace BookingService.API;

public static class BookingApi
{

    public static RouteGroupBuilder MapBookingApiV1(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("api/v1/booking").WithTags("Booking API");

        api.MapPost("/from-reservation", CreateBookingAsync);//.RequireAuthorization(PermissionPolicies.Require("booking.write"));
        api.MapPost("/draft", CreateBookingDraftAsync)
            // Draft is an internal/legacy operation and is intentionally absent from client OpenAPI.
            .ExcludeFromDescription();//.RequireAuthorization(PermissionPolicies.Require("booking.write"));

        // Begin check out, nên là put vì chuyển status booking sang awaiting payment
        api.MapPut("/payment", ChangeToAwaitingPaymentAsync);//.RequireAuthorization(PermissionPolicies.Require("booking.write"));

        api.MapGet("/cardtype", GetCardTypeAsync);//.RequireAuthorization(PermissionPolicies.Require("booking.read"));
        
        // // Endpoint get booking by userId
        api.MapGet("/{userId}", GetBookingByUserAsync);//.RequireAuthorization(PermissionPolicies.Require("booking.read"));

        // // Get booking by id
        api.MapGet("/{bookingid:int}", GetBookingAsync);//.RequireAuthorization(PermissionPolicies.Require("booking.read"));

        return api;
    }

    public static async Task<BookingDraftDto> CreateBookingDraftAsync(
        CreateBookingDraftCommand command,
        SeatGrpc.SeatGrpcClient seatClient,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var requestedSeats = command.seats.ToArray();
        var showtimeIds = requestedSeats
            .Select(seat => seat.ShowtimeId)
            .Distinct();

        var snapshotTasks = showtimeIds.Select(async showtimeId =>
        {
            var snapshot = await seatClient.GetShowtimeSeatsAsync(
                new GetShowtimeSeatsRequest { ShowtimeId = showtimeId.ToString() },
                cancellationToken: cancellationToken);

            return (showtimeId, seatIds: snapshot.Seats.Select(seat => seat.SeatId).ToHashSet());
        });

        var snapshots = (await Task.WhenAll(snapshotTasks))
            .ToDictionary(snapshot => snapshot.showtimeId, snapshot => snapshot.seatIds);

        var snapshotSeats = requestedSeats.Where(seat =>
            snapshots.TryGetValue(seat.ShowtimeId, out var seatIds) &&
            seatIds.Contains(seat.SeatId));

        return await mediator.Send(command with { seats = snapshotSeats }, cancellationToken);
    }

    // public static async Task<Results<Ok<string>, BadRequest<string>>> ChangeToAwaitingPaymentAsync(...)
    public static async Task<Results<Ok<string>, ProblemHttpResult>> ChangeToAwaitingPaymentAsync(SetAwaitingPaymentBookingStatusCommand command, IMediator mediator)
    {
        var result = await mediator.Send(command);
        if (result)
        {
            return TypedResults.Ok($"Change status waiting payment success");
        } 
        else
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Booking cannot start payment",
                detail: "The booking could not be changed to awaiting-payment status.");
        }
    }

    // Old response only returned a text message containing RequestId, so the app
    // could not call the payment endpoint which requires BookingId.
    // public static async Task<Results<Ok<string>, BadRequest<string>>> CreateBookingAsync(...)
    public static async Task<Results<Created<CreateBookingResponse>, ProblemHttpResult>> CreateBookingAsync(
        [FromBody] FromReservationRequest request,
        SeatGrpc.SeatGrpcClient seatClient,
        IMediator mediator,
        IBookingQueries bookingQueries)
    {
        // Gọi Seat Service để check data
        var validation = await seatClient.ValidationReservationAsync(new ValidationReservationRequest
        {
            ShowtimeId = request.showtimeId,
            ReservationId = request.reservationId.ToString(),
            UserId = request.userId
        });

        if (!validation.Success)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Seat reservation is invalid",
                detail: $"The reservation could not be validated. ReservationId: {request.reservationId}");
        }

        // Lấy dữ liệu từ seat service
        var bookingItems = validation.SeatIds.Select(seatId => new SeatItem
        {
            ShowtimeId = validation.ShowtimeId,
            SeatId = seatId,
            BasePrice = Convert.ToDecimal(validation.BasePrice)
        });

        var createBookingCommand = new CreateBookingCommand(bookingItems, request.userId, request.userName, request.showtimeId, request.reservationId);

        var requestId = Guid.NewGuid();
        var requestBooking = new IdentifiedCommand<CreateBookingCommand, bool>(createBookingCommand, requestId);

        var result = await mediator.Send(requestBooking);

        if (result)
        {
            var bookingId = await bookingQueries.GetBookingIdByReservationAsync(
                request.reservationId);

            if (!bookingId.HasValue)
            {
                return TypedResults.Problem(
                    statusCode: StatusCodes.Status500InternalServerError,
                    title: "Booking could not be loaded",
                    detail: $"The booking was created but could not be loaded. ReservationId: {request.reservationId}");
            }


            // return TypedResults.Ok($"CreateBookingCommand succeeded - RequestId: {requestId}");
            return TypedResults.Created(
                $"/api/vi/booking/{bookingId.Value}",
                new CreateBookingResponse(
                    bookingId.Value,
                    request.reservationId,
                    requestId,
                    "Submitted"));
        }
        else
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Booking could not be created",
                detail: $"The booking request failed. RequestId: {requestId}");
        }
    }

    public static async Task<Ok<IEnumerable<CardTypeVM>>> GetCardTypeAsync([AsParameters] BookingService bookingService)
    {
        var cardType = await bookingService.BookingQueries.GetCardTypesAsync();
        return TypedResults.Ok(cardType);
    }

    public static async Task<Results<Ok<BookingVM>, ProblemHttpResult>> GetBookingAsync (int bookingId, [AsParameters] BookingService bookingService)
    {
        try
        {
            var booking = await bookingService.BookingQueries.GetBookingAsync(bookingId);
            return TypedResults.Ok(booking);
        } catch
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Booking not found",
                detail: $"Booking {bookingId} does not exist.");
        }
    }
    public static async Task<Results<Ok<IEnumerable<BookingVM>>, ProblemHttpResult>> GetBookingByUserAsync (string userId, [AsParameters] BookingService bookingService)
    {
        // Tam thoi truyen user id thong qua parameter, khi cos identity, lay userid qua indentityservice
        var booking = await bookingService.BookingQueries.GetBookingFromUserAsync(userId);
        if(booking is null)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Bookings not found",
                detail: $"No booking was found for user {userId}.");
        }
        return TypedResults.Ok(booking);
    }
    
}

public sealed record CreateBookingRequest
{
    public string UserId { get; init; } = string.Empty;
    public int ShowtimeId { get; init; }
    public int HallId { get; init; }
    public IEnumerable<SeatItem> BookingItem { get; init; } = [];
}

public record FromReservationRequest
{
    public int showtimeId {get; init;}
    public string userId {get; init;} = string.Empty;
    public string userName {get; init;} = string.Empty;
    public Guid reservationId {get ; init;}
    public IEnumerable<SeatItem> BookingItem { get; init; } = [];

}

public sealed record CreateBookingResponse(
    int BookingId,
    Guid ReservationId,
    Guid RequestId,
    string Status);
