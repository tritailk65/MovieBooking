// using Grpc.Net.Client;
// using MediatR;
// using Microsoft.AspNetCore.Builder;
// using Microsoft.AspNetCore.Hosting;
// using Microsoft.AspNetCore.TestHost;
// using Microsoft.Extensions.DependencyInjection;
// using Seat.API.Grpc;

// namespace Seat.API.UnitTests.Apis;

// public class SeatGrpcIntegrationTests
// {
//     [Fact]
//     public async Task ValidationReservation_ThroughGrpcPipeline_ShouldReturnReservation()
//     {
//         var mediator = Substitute.For<IMediator>();
//         var reservationId = Guid.NewGuid();

//         mediator
//             .Send(Arg.Any<ValidationReservationCommand>(), Arg.Any<CancellationToken>())
//             .Returns(new SeatReservation
//             {
//                 Id = reservationId,
//                 ShowtimeId = 10,
//                 UserId = "user-1",
//                 SeatIds = ["A1", "A2"],
//                 RemainingSeconds = 120,
//                 BasePrice = 180_000m
//             });

//         await using var app = await CreateGrpcAppAsync(mediator);
//         using var channel = CreateGrpcChannel(app);
//         var client = new SeatGrpc.SeatGrpcClient(channel);

//         var response = await client.ValidationReservationAsync(
//             new ValidationReservationRequest
//             {
//                 ShowtimeId = 10,
//                 ReservationId = reservationId.ToString(),
//                 UserId = "user-1"
//             });

//         Assert.True(response.Success);
//         Assert.Equal(reservationId.ToString(), response.ReservationId);
//         Assert.Equal(["A1", "A2"], response.SeatIds);
//         Assert.Equal(180_000d, response.BasePrice);

//         await mediator.Received(1).Send(
//             Arg.Is<ValidationReservationCommand>(command =>
//                 command.showtimeId == 10 &&
//                 command.reservationId == reservationId.ToString() &&
//                 command.userId == "user-1"),
//             Arg.Any<CancellationToken>());
//     }

//     [Fact]
//     public async Task ReleaseSeatReservation_ThroughGrpcPipeline_ShouldReturnSuccess()
//     {
//         var mediator = Substitute.For<IMediator>();
//         var reservationId = Guid.NewGuid().ToString();

//         mediator
//             .Send(Arg.Any<ReleaseSeatReservationCommand>(), Arg.Any<CancellationToken>())
//             .Returns(true);

//         await using var app = await CreateGrpcAppAsync(mediator);
//         using var channel = CreateGrpcChannel(app);
//         var client = new SeatGrpc.SeatGrpcClient(channel);

//         var response = await client.ReleaseSeatReservationAsync(
//             new ReleaseSeatReservationRequest
//             {
//                 ShowtimeId = 10,
//                 ReservationId = reservationId,
//                 UserId = "user-1"
//             });

//         Assert.True(response.Success);

//         await mediator.Received(1).Send(
//             Arg.Is<ReleaseSeatReservationCommand>(command =>
//                 command.showtimeId == 10 &&
//                 command.reservationId == reservationId &&
//                 command.userId == "user-1"),
//             Arg.Any<CancellationToken>());
//     }

//     private static async Task<WebApplication> CreateGrpcAppAsync(IMediator mediator)
//     {
//         var builder = WebApplication.CreateBuilder();

//         builder.WebHost.UseTestServer();
//         builder.Services.AddGrpc();
//         builder.Services.AddSingleton(mediator);

//         var app = builder.Build();
//         app.MapGrpcService<SeatService>();

//         await app.StartAsync();

//         return app;
//     }

//     private static GrpcChannel CreateGrpcChannel(WebApplication app)
//     {
//         return GrpcChannel.ForAddress(
//             "http://localhost",
//             new GrpcChannelOptions
//             {
//                 HttpHandler = app.GetTestServer().CreateHandler()
//             });
//     }
// }
