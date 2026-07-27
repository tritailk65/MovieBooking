namespace Seat.API.UnitTests.Application.Seats;

public class LockSeatCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenSeatIsAvailable_ShouldLockSeatAndCreateReservation()
    {
        var redis = Substitute.For<IConnectionMultiplexer>();
        var logger = Substitute.For<ILogger<LockSeatCommandHandler>>();
        var lockService = Substitute.For<IRedisLockService>();
        var seatRepo = Substitute.For<ISeatRepository>();

        var command = new LockSeatCommand(
            showtimeId: 1,
            seatId: "A1",
            userId: "user-1"
        );

        var availableSeat = new Domain.Entities.Seat
        {
            ShowtimeId = 1,
            SeatId = "A1",
            SeatStatus = SeatStatus.Available,
            BasePrice = 90000m
        };

        // lấy mutex-lock
        lockService
            .AcquireLockAsync(1, "A1", Arg.Any<TimeSpan>())
            .Returns("mutex-token");

        lockService
            .GetLockSeatAsync(1, "A1")
            .Returns((Domain.Entities.Seat?)null);

        seatRepo
            .GetSeatHashAsync(1, "A1")
            .Returns(availableSeat);

        seatRepo
            .GetSeatReservationHashAsync(1, "user-1")
            .Returns((SeatReservation?)null);

        lockService
            .SetLockSeatAsync(1, "A1", Arg.Any<string>(), Arg.Any<TimeSpan>())
            .Returns(true);

        lockService
            .ReleaseMutexAsync(1, "A1", "mutex-token")
            .Returns(true);

        var handler = new LockSeatCommandHandler(
            redis,
            logger,
            lockService,
            seatRepo
        );

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(string.IsNullOrWhiteSpace(result.LockToken));
        Assert.True(result.LockExpiration > DateTime.UtcNow);

        await seatRepo.Received(1)
            .SetSeatReservationHashAsync(
                Arg.Is<SeatReservation>(r =>
                    r.ShowtimeId == 1 &&
                    r.UserId == "user-1" &&
                    r.SeatIds.Contains("A1")));

        await seatRepo.Received(1)
            .SetSeatHashAysnc(1, "A1", Arg.Any<string>());

        await lockService.Received(1)
            .ReleaseMutexAsync(1, "A1", "mutex-token");

    }

    [Fact]
    public async Task Handle_WhenMutexCannotBeAcquired_ShouldReturnFailedResult()
    {
        var redis = Substitute.For<IConnectionMultiplexer>();
        var logger = Substitute.For<ILogger<LockSeatCommandHandler>>();
        var lockService = Substitute.For<IRedisLockService>();
        var seatRepo = Substitute.For<ISeatRepository>();

        var command = new LockSeatCommand(
            showtimeId: 1,
            seatId: "A1",
            userId: "user-1"
        );

        lockService
            .AcquireLockAsync(1, "A1", Arg.Any<TimeSpan>())
            .Returns(string.Empty);

        var handler = new LockSeatCommandHandler(
            redis,
            logger,
            lockService,
            seatRepo
        );

        var result = await handler.Handle(command, CancellationToken.None);


        Assert.False(result.IsSuccess);
            await seatRepo.DidNotReceive()
                .SetSeatHashAysnc(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>());
    }
}