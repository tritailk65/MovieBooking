using MediatR;
using Seat.API.Grpc;

namespace Seat.API.UnitTests.Apis;

public class SeatGrpcServiceTests
{
    [Fact]
    public async Task ValidationReservation_WhenReservationExists_ShouldReturnMappedResponse()
    {
        var mediator = Substitute.For<IMediator>();
        var logger = Substitute.For<ILogger<SeatService>>();
        var reservationId = Guid.NewGuid();
        var request = new ValidationReservationRequest
        {
            ShowtimeId = 10,
            ReservationId = reservationId.ToString(),
            UserId = "user-1"
        };
        var reservation = new SeatReservation
        {
            Id = reservationId,
            ShowtimeId = 10,
            UserId = "user-1",
            SeatIds = ["A1", "A2"],
            RemainingSeconds = 120,
            BasePrice = 180_000m
        };

        mediator
            .Send(Arg.Any<ValidationReservationCommand>(), Arg.Any<CancellationToken>())
            .Returns(reservation);

        var service = new SeatService(mediator, logger);

        var response = await service.ValidationReservation(request, null!);

        Assert.True(response.Success);
        Assert.Equal(reservationId.ToString(), response.ReservationId);
        Assert.Equal(10, response.ShowtimeId);
        Assert.Equal("user-1", response.UserId);
        Assert.Equal(["A1", "A2"], response.SeatIds);
        Assert.Equal(120, response.RemainingSeconds);
        Assert.Equal(180_000d, response.BasePrice);

        await mediator.Received(1).Send(
            Arg.Is<ValidationReservationCommand>(command =>
                command.showtimeId == 10 &&
                command.reservationId == reservationId.ToString() &&
                command.userId == "user-1"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ValidationReservation_WhenReservationDoesNotExist_ShouldReturnFailedResponse()
    {
        var mediator = Substitute.For<IMediator>();
        var logger = Substitute.For<ILogger<SeatService>>();
        var request = new ValidationReservationRequest
        {
            ShowtimeId = 10,
            ReservationId = Guid.NewGuid().ToString(),
            UserId = "user-1"
        };

        mediator
            .Send(Arg.Any<ValidationReservationCommand>(), Arg.Any<CancellationToken>())
            .Returns((SeatReservation?)null);

        var service = new SeatService(mediator, logger);

        var response = await service.ValidationReservation(request, null!);

        Assert.False(response.Success);
        Assert.Empty(response.ReservationId);
        Assert.Empty(response.SeatIds);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ReleaseSeatReservation_ShouldReturnMediatorResult(bool released)
    {
        var mediator = Substitute.For<IMediator>();
        var logger = Substitute.For<ILogger<SeatService>>();
        var reservationId = Guid.NewGuid().ToString();
        var request = new ReleaseSeatReservationRequest
        {
            ShowtimeId = 10,
            ReservationId = reservationId,
            UserId = "user-1"
        };

        mediator
            .Send(Arg.Any<ReleaseSeatReservationCommand>(), Arg.Any<CancellationToken>())
            .Returns(released);

        var service = new SeatService(mediator, logger);

        var response = await service.ReleaseSeatReservation(request, null!);

        Assert.Equal(released, response.Success);

        await mediator.Received(1).Send(
            Arg.Is<ReleaseSeatReservationCommand>(command =>
                command.showtimeId == 10 &&
                command.reservationId == reservationId &&
                command.userId == "user-1"),
            Arg.Any<CancellationToken>());
    }
}
