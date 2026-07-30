namespace Booking.API.UnitTests.Saga;

using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Orchestration.Tests.NUnit;
using SagaOrchestration;
using SagaOrchestration.Contract;

[TestFixture]
public class BookingSagaStateMachineSpec
{
    private ServiceProvider _provider = null!;
    private ITestHarness _harness = null!;
    private ISagaStateMachineTestHarness<BookingStateMachine, BookingSaga> _sagaHarness = null!;

    private static readonly Guid ReservationId = Guid.Parse("b185922e-3061-49a1-a9e6-28521eeca2f9");
    private const int BookingId = 99;
    private const int ShowtimeId = 1;
    private const string UserId = "2779fb04-052e-49c1-8ce0-c200d8e06b6f";
    private const decimal TotalPrice = 180_000m;

    [SetUp]
    public async Task Setup()
    {
        _provider = new ServiceCollection()
            .ConfigureMassTransit(x =>
            {
                x.AddSagaStateMachine<BookingStateMachine, BookingSaga>();
            })
            .BuildServiceProvider(true);

        _harness = _provider.GetTestHarness();
        await _harness.Start();
        _sagaHarness = _harness.GetSagaStateMachineHarness<BookingStateMachine, BookingSaga>();
    }

    [TearDown]
    public async Task Teardown()
    {
        await _harness.Stop();
        await _provider.DisposeAsync();
    }

    [Test]
    public async Task BookingSubmitted_ShouldCreateSagaAndRequestPayment()
    {
        await PublishBookingSubmitted();

        Assert.That(
            await _sagaHarness.Exists(ReservationId, state => state.PaymentPending),
            Is.Not.Null);

        Assert.That(
            await _harness.Published.Any<RequestPaymentCommand>(message =>
                message.Context.Message.ReservationId == ReservationId &&
                message.Context.Message.BookingId == BookingId &&
                message.Context.Message.Amount == TotalPrice),
            Is.True);

        var saga = _sagaHarness.Sagas.Contains(ReservationId);
        Assert.That(saga, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(saga!.BookingId, Is.EqualTo(BookingId));
            Assert.That(saga.ShowtimeId, Is.EqualTo(ShowtimeId));
            Assert.That(saga.UserId, Is.EqualTo(UserId));
            Assert.That(saga.Seats, Is.EqualTo(new[] { "A1", "A2" }));
            Assert.That(saga.ReservationVersion, Is.EqualTo(3));
        });
    }

    [Test]
    public async Task PaymentSucceeded_ShouldRequestSeatConfirmation()
    {
        await PublishBookingSubmitted();

        await _harness.Bus.Publish(new PaymentSucceededIntegrationEvent(
            ReservationId,
            BookingId,
            "payment-123"));

        Assert.That(
            await _sagaHarness.Exists(ReservationId, state => state.ConfirmingSeats),
            Is.Not.Null);

        Assert.That(
            await _harness.Published.Any<ConfirmSeatReservationCommand>(message =>
                message.Context.Message.ReservationId == ReservationId &&
                message.Context.Message.BookingId == BookingId &&
                message.Context.Message.ShowtimeId == ShowtimeId &&
                message.Context.Message.ReservationVersion == 3),
            Is.True);
    }

    [Test]
    public async Task PaymentFailed_ShouldCancelBookingAndReleaseReservation()
    {
        await PublishBookingSubmitted();

        await _harness.Bus.Publish(new PaymentFailedIntegrationEvent(
            ReservationId,
            BookingId,
            "Card declined"));

        Assert.That(
            await _sagaHarness.Exists(ReservationId, state => state.Cancelling),
            Is.Not.Null);
        Assert.That(
            await _harness.Published.Any<CancelBookingCommand>(message =>
                message.Context.Message.ReservationId == ReservationId),
            Is.True);
        Assert.That(
            await _harness.Published.Any<ReleaseSeatReservationCommand>(message =>
                message.Context.Message.ReservationId == ReservationId),
            Is.True);

        await _harness.Bus.Publish(new BookingCancelledIntegrationEvent(ReservationId, BookingId));
        await _harness.Bus.Publish(new SeatReservationReleasedIntegrationEvent(ReservationId, BookingId));

        Assert.That(
            await _sagaHarness.Consumed.Any<SeatReservationReleasedIntegrationEvent>(),
            Is.True);
        Assert.That(
            await _sagaHarness.NotExists(ReservationId),
            Is.Null);
    }

    [Test]
    public async Task SeatConfirmationFailed_ShouldRunAllCompensations()
    {
        await PublishBookingSubmitted();
        await _harness.Bus.Publish(new PaymentSucceededIntegrationEvent(
            ReservationId,
            BookingId,
            "payment-123"));
        await _harness.Bus.Publish(new SeatReservationConfirmationFailedIntegrationEvent(
            ReservationId,
            BookingId,
            "Redis unavailable"));

        Assert.That(
            await _sagaHarness.Exists(ReservationId, state => state.Compensating),
            Is.Not.Null);
        Assert.That(await _harness.Published.Any<RefundPaymentCommand>(), Is.True);
        Assert.That(await _harness.Published.Any<CancelBookingCommand>(), Is.True);
        Assert.That(await _harness.Published.Any<ReleaseSeatReservationCommand>(), Is.True);

        await _harness.Bus.Publish(new PaymentRefundedIntegrationEvent(
            ReservationId,
            BookingId,
            "payment-123"));
        await _harness.Bus.Publish(new BookingCancelledIntegrationEvent(ReservationId, BookingId));
        await _harness.Bus.Publish(new SeatReservationReleasedIntegrationEvent(ReservationId, BookingId));

        Assert.That(
            await _sagaHarness.Consumed.Any<SeatReservationReleasedIntegrationEvent>(),
            Is.True);
        Assert.That(
            await _sagaHarness.NotExists(ReservationId),
            Is.Null);
    }

    private Task PublishBookingSubmitted() =>
        _harness.Bus.Publish(new BookingSubmittedIntegrationEvent(
            ReservationId,
            BookingId,
            ShowtimeId,
            UserId,
            new[] { "A1", "A2" },
            TotalPrice,
            ReservationVersion: 3,
            PreparedUntil: DateTime.UtcNow.AddMinutes(5)));
}

[TestFixture]
public class BookingSagaHappyPathIntegrationSpec
{
    private ServiceProvider _provider = null!;
    private ITestHarness _harness = null!;
    private ISagaStateMachineTestHarness<BookingStateMachine, BookingSaga> _sagaHarness = null!;

    [SetUp]
    public async Task Setup()
    {
        _provider = new ServiceCollection()
            .ConfigureMassTransit(x =>
            {
                x.AddSagaStateMachine<BookingStateMachine, BookingSaga>();
                x.AddConsumer<FakePaymentConsumer>();
                x.AddConsumer<FakeSeatConsumer>();
                x.AddConsumer<FakeBookingConsumer>();
            })
            .BuildServiceProvider(true);

        _harness = _provider.GetTestHarness();
        await _harness.Start();
        _sagaHarness = _harness.GetSagaStateMachineHarness<BookingStateMachine, BookingSaga>();
    }

    [TearDown]
    public async Task Teardown()
    {
        await _harness.Stop();
        await _provider.DisposeAsync();
    }

    [Test]
    public async Task BookingSubmitted_ShouldCompleteTheWholeOrchestration()
    {
        var reservationId = Guid.NewGuid();

        await _harness.Bus.Publish(new BookingSubmittedIntegrationEvent(
            reservationId,
            BookingId: 99,
            ShowtimeId: 1,
            UserId: "user-1",
            SeatIds: new[] { "A1", "A2" },
            TotalPrice: 180_000m,
            ReservationVersion: 3,
            PreparedUntil: DateTime.UtcNow.AddMinutes(5)));

        Assert.That(
            await _harness.Consumed.Any<RequestPaymentCommand>(),
            Is.True,
            "Fake Payment consumer did not receive the payment command");
        Assert.That(
            await _harness.Consumed.Any<ConfirmSeatReservationCommand>(),
            Is.True,
            "Fake Seat consumer did not receive the confirmation command");
        Assert.That(
            await _harness.Consumed.Any<MarkBookingPaidCommand>(),
            Is.True,
            "Fake Booking consumer did not receive the mark-paid command");
        Assert.That(
            await _sagaHarness.Consumed.Any<BookingMarkedPaidIntegrationEvent>(),
            Is.True,
            "Saga did not receive the final Booking acknowledgement");

        Assert.That(
            await _sagaHarness.NotExists(reservationId),
            Is.Null);
        Assert.That(await _harness.Published.Any<Fault>(), Is.False);
    }

    private sealed class FakePaymentConsumer : IConsumer<RequestPaymentCommand>
    {
        public Task Consume(ConsumeContext<RequestPaymentCommand> context) =>
            context.Publish(new PaymentSucceededIntegrationEvent(
                context.Message.ReservationId,
                context.Message.BookingId,
                "payment-integration-test"));
    }

    private sealed class FakeSeatConsumer : IConsumer<ConfirmSeatReservationCommand>
    {
        public Task Consume(ConsumeContext<ConfirmSeatReservationCommand> context) =>
            context.Publish(new SeatReservationConfirmedIntegrationEvent(
                context.Message.ReservationId,
                context.Message.BookingId));
    }

    private sealed class FakeBookingConsumer : IConsumer<MarkBookingPaidCommand>
    {
        public Task Consume(ConsumeContext<MarkBookingPaidCommand> context) =>
            context.Publish(new BookingMarkedPaidIntegrationEvent(
                context.Message.ReservationId,
                context.Message.BookingId));
    }
}
