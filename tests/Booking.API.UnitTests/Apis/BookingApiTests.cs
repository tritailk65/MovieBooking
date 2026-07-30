// using System.Net;
// using System.Net.Http.Json;
// using BookingService.API;
// using BookingService.API.Application.Commands.CreateBooking;
// using BookingService.API.Application.Commands.Identified;
// using Grpc.Core;
// using MediatR;
// using Microsoft.AspNetCore.Builder;
// using Microsoft.AspNetCore.Hosting;
// using Microsoft.AspNetCore.TestHost;
// using Microsoft.Extensions.DependencyInjection;
// using NSubstitute;
// using Seat.API.Grpc;

// namespace Booking.API.UnitTests.Apis;

// public class BookingApiTests
// {
//     [Fact]
//     public async Task CreateBookingFromReservation_WhenSeatGrpcValidatesReservation_ShouldCreateBooking()
//     {
//         var mediator = Substitute.For<IMediator>();
//         var seatStub = new SeatGrpcStub
//         {
//             Response = new ValidationReservationResponse
//             {
//                 Success = true,
//                 ReservationId = Guid.NewGuid().ToString(),
//                 ShowtimeId = 10,
//                 UserId = "user-1",
//                 RemainingSeconds = 120,
//                 BasePrice = 90_000d
//             }
//         };
//         seatStub.Response.SeatIds.AddRange(["A1", "A2"]);

//         mediator
//             .Send(
//                 Arg.Any<IdentifiedCommand<CreateBookingCommand, bool>>(),
//                 Arg.Any<CancellationToken>())
//             .Returns(true);

//         await using var seatApp = await CreateSeatGrpcAppAsync(seatStub);
//         await using var bookingApp = await CreateBookingAppAsync(mediator, seatApp);
//         var bookingClient = bookingApp.GetTestClient();
//         var reservationId = Guid.Parse(seatStub.Response.ReservationId);

//         var response = await bookingClient.PostAsJsonAsync(
//             "/api/booking/from-reservation",
//             new FromReservationRequest
//             {
//                 showtimeId = 10,
//                 userId = "user-1",
//                 userName = "Test User",
//                 reservationId = reservationId
//             });

//         Assert.Equal(HttpStatusCode.OK, response.StatusCode);
//         Assert.NotNull(seatStub.LastRequest);
//         Assert.Equal(10, seatStub.LastRequest.ShowtimeId);
//         Assert.Equal("user-1", seatStub.LastRequest.UserId);
//         Assert.Equal(reservationId.ToString(), seatStub.LastRequest.ReservationId);

//         await mediator.Received(1).Send(
//             Arg.Is<IdentifiedCommand<CreateBookingCommand, bool>>(identified =>
//                 identified.Command.UserId == "user-1" &&
//                 identified.Command.UserName == "Test User" &&
//                 identified.Command.ShowtimeId == 10 &&
//                 identified.Command.ReservationId == reservationId &&
//                 identified.Command.BookingItem.Count() == 2 &&
//                 identified.Command.BookingItem.All(item =>
//                     item.ShowtimeId == 10 &&
//                     item.BasePrice == 90_000m) &&
//                 identified.Command.BookingItem.Select(item => item.SeatId)
//                     .SequenceEqual(new[] { "A1", "A2" })),
//             Arg.Any<CancellationToken>());
//     }

//     [Fact]
//     public async Task CreateBookingFromReservation_WhenSeatGrpcRejectsReservation_ShouldReturnBadRequest()
//     {
//         var mediator = Substitute.For<IMediator>();
//         var seatStub = new SeatGrpcStub
//         {
//             Response = new ValidationReservationResponse { Success = false }
//         };

//         await using var seatApp = await CreateSeatGrpcAppAsync(seatStub);
//         await using var bookingApp = await CreateBookingAppAsync(mediator, seatApp);
//         var bookingClient = bookingApp.GetTestClient();

//         var response = await bookingClient.PostAsJsonAsync(
//             "/api/booking/from-reservation",
//             new FromReservationRequest
//             {
//                 showtimeId = 10,
//                 userId = "user-1",
//                 userName = "Test User",
//                 reservationId = Guid.NewGuid()
//             });

//         Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
//         Assert.NotNull(seatStub.LastRequest);

//         await mediator.DidNotReceive().Send(
//             Arg.Any<IdentifiedCommand<CreateBookingCommand, bool>>(),
//             Arg.Any<CancellationToken>());
//     }

//     private static async Task<WebApplication> CreateSeatGrpcAppAsync(SeatGrpcStub seatStub)
//     {
//         var builder = WebApplication.CreateBuilder();

//         builder.WebHost.UseTestServer();
//         builder.Services.AddGrpc();
//         builder.Services.AddSingleton(seatStub);

//         var app = builder.Build();
//         app.MapGrpcService<SeatGrpcStub>();

//         await app.StartAsync();

//         return app;
//     }

//     private static async Task<WebApplication> CreateBookingAppAsync(
//         IMediator mediator,
//         WebApplication seatApp)
//     {
//         var builder = WebApplication.CreateBuilder();

//         builder.WebHost.UseTestServer();
//         builder.Services.AddSingleton(mediator);
//         builder.Services
//             .AddGrpcClient<SeatGrpc.SeatGrpcClient>(options =>
//                 options.Address = new Uri("http://seat-api"))
//             .ConfigurePrimaryHttpMessageHandler(
//                 () => seatApp.GetTestServer().CreateHandler());

//         var app = builder.Build();
//         app.MapPost(
//             "/api/booking/from-reservation",
//             BookingService.API.BookingApi.CreateBookingAsync);

//         await app.StartAsync();

//         return app;
//     }

//     public sealed class SeatGrpcStub : SeatGrpc.SeatGrpcBase
//     {
//         public ValidationReservationResponse Response { get; init; } = new();
//         public ValidationReservationRequest? LastRequest { get; private set; }

//         public override Task<ValidationReservationResponse> ValidationReservation(
//             ValidationReservationRequest request,
//             ServerCallContext context)
//         {
//             LastRequest = request;
//             return Task.FromResult(Response);
//         }
//     }
// }
