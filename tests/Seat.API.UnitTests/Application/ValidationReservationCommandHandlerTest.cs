
namespace Seat.API.UnitTests.Application.Seats;

public class ValidationReservationCommandHandlerTest
{

    [Fact]
    public async Task Handle_WhenReservationIsValid_ShouldReturnReservationWithRemainingSeconds()
    {
        var logger = Substitute.For<ILogger<ValidationReservationCommandHandler>>();
        var lockService = Substitute.For<IRedisLockService>();
        var seatRepo = Substitute.For<ISeatRepository>();

        var reservationId = Guid.NewGuid();

        var reservation = new SeatReservation(
            reservationId,
            showtimeId: 1,
            userId: "user-1",
            seatIds: ["A1", "A2"],
            expiresAt: DateTime.UtcNow.AddMinutes(5)
        );

        seatRepo
            .GetSeatReservationHashAsync(1, "user-1")
            .Returns(reservation);

        lockService
            .GetLockSeatAsync(1, "A1")
            .Returns(new Domain.Entities.Seat { ShowtimeId = 1, SeatId = "A1", LockedByUserId = "user-1" });

        lockService
            .GetLockSeatAsync(1, "A2")
            .Returns(new Domain.Entities.Seat { ShowtimeId = 1, SeatId = "A2", LockedByUserId = "user-1" });

        var handler = new ValidationReservationCommandHandler(
            logger,
            lockService,
            seatRepo
        );

        var command = new ValidationReservationCommand(
            showtimeId: 1,
            reservationId: reservationId.ToString(),
            userId: "user-1"
        );

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(reservationId, result.Id);
        Assert.True(result.RemainingSeconds > 0);
    }
}


