namespace Booking.API.UnitTests.Saga;

[TestFixture]
public class BookingSagaStateMachineSadPathSpec
{
    private ServiceProvider _provider = null!;
    private ITestHarness _harness = null!;
    private ISagaStateMachineTestHarness<BookingStateMachine, BookingSaga> _sagaHarness = null!;

    private const int ShowtimeId = 1;
    private const string UserId = "2779fb04-052e-49c1-8ce0-c200d8e06b6f";
    private const decimal TotalPrice = 180_000m;

    private sealed record SagaTestContext(
        Guid ReservationId,
        int BookingId,
        string PaymentId);

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

    private async Task<SagaTestContext> CreateSagaInReservingSeat()
    {
        var context = new SagaTestContext(
            NewId.NextGuid(),
            Random.Shared.Next(1, int.MaxValue),
            NewId.NextGuid().ToString());

        await _harness.Bus.Publish(new BookingStatusChangedToSubmittedIntegrationEvent(
            context.ReservationId,
            context.BookingId,
            ShowtimeId,
            UserId,
            ["A1", "A2"],
            TotalPrice,
            ReservationVersion: 3,
            PreparedUntil: DateTime.UtcNow.AddMinutes(5)));

        Assert.That(
            await _sagaHarness.Exists(context.ReservationId, state => state.ReservingSeat),
            Is.EqualTo(context.ReservationId),
            "Saga not transition to ReservingSeat");

        return context;
    }

    private async Task<SagaTestContext> CreateSagaInPendingPayment()
    {
        var context = await CreateSagaInReservingSeat();

        await _harness.Bus.Publish(new SeatReservationHeldIntegrationEvent(
            context.ReservationId,
            context.BookingId));

        Assert.That(
            await _sagaHarness.Exists(context.ReservationId, state => state.PendingPayment),
            Is.EqualTo(context.ReservationId),
            "Saga not transition to PendingPayment");

        return context;
    }

    private async Task<SagaTestContext> CreateSagaInExtendingSeatHold()
    {
        var context = await CreateSagaInPendingPayment();

        await _harness.Bus.Publish(new PaymentRequestedIntegrationEvent(
            context.ReservationId,
            context.BookingId,
            UserId,
            TotalPrice));

        Assert.That(
            await _sagaHarness.Exists(context.ReservationId, state => state.ExtendingSeatHold),
            Is.EqualTo(context.ReservationId),
            "Saga not transition to ExtendingSeatHold");

        return context;
    }

    private async Task<SagaTestContext> CreateSagaInPaymentProcessing()
    {
        var context = await CreateSagaInExtendingSeatHold();

        await _harness.Bus.Publish(new SeatHoldExtendedIntegrationEvent(
            context.ReservationId,
            context.BookingId));

        Assert.That(
            await _sagaHarness.Exists(context.ReservationId, state => state.PaymentProcessing),
            Is.EqualTo(context.ReservationId),
            "Saga not transition to PaymentProcessing");

        return context;
    }

    private async Task<SagaTestContext> CreateSagaInReconcilingPayment()
    {
        var context = await CreateSagaInPaymentProcessing();

        await _harness.Bus.Publish(new PaymentTimedOutIntegrationEvent(
            context.ReservationId,
            context.BookingId));

        Assert.That(
            await _sagaHarness.Exists(context.ReservationId, state => state.ReconcilingPayment),
            Is.EqualTo(context.ReservationId),
            "Saga not transition to ReconcilingPayment");

        return context;
    }

    private async Task<SagaTestContext> CreateSagaInResolvingUnknownPayment()
    {
        var context = await CreateSagaInReconcilingPayment();

        await _harness.Bus.Publish(new SeatHoldDeadlineReachedIntegrationEvent(
            context.ReservationId,
            context.BookingId));

        Assert.That(
            await _sagaHarness.Exists(context.ReservationId, state => state.ResolvingUnknownPayment),
            Is.EqualTo(context.ReservationId),
            "Saga not transition to ResolvingUnknownPayment");

        return context;
    }

    private async Task<SagaTestContext> CreateSagaInConfirmingSeats()
    {
        var context = await CreateSagaInPaymentProcessing();

        await _harness.Bus.Publish(new PaymentSucceededIntegrationEvent(
            context.ReservationId,
            context.BookingId,
            context.PaymentId));

        Assert.That(
            await _sagaHarness.Exists(context.ReservationId, state => state.ConfirmingSeats),
            Is.EqualTo(context.ReservationId),
            "Saga not transition to ConfirmingSeats");

        return context;
    }

    private async Task<SagaTestContext> CreateSagaInCompletingBooking()
    {
        var context = await CreateSagaInConfirmingSeats();

        await _harness.Bus.Publish(new SeatReservationConfirmedIntegrationEvent(
            context.ReservationId,
            context.BookingId));

        Assert.That(
            await _sagaHarness.Exists(context.ReservationId, state => state.CompletingBooking),
            Is.EqualTo(context.ReservationId),
            "Saga not transition to CompletingBooking");

        return context;
    }

    private async Task<SagaTestContext> CreateSagaInCompensating()
    {
        var context = await CreateSagaInConfirmingSeats();

        await _harness.Bus.Publish(new SeatReservationConfirmationFailedIntegrationEvent(
            context.ReservationId,
            context.BookingId,
            "Cannot confirm seats"));

        Assert.That(
            await _sagaHarness.Exists(context.ReservationId, state => state.Compensating),
            Is.EqualTo(context.ReservationId),
            "Saga not transition to Compensating");

        return context;
    }

    private async Task<SagaTestContext> CreateSagaInCancelling()
    {
        var context = await CreateSagaInReservingSeat();

        await _harness.Bus.Publish(new SeatReservationFailedIntegrationEvent(
            context.ReservationId,
            context.BookingId,
            "Cannot reserve seats"));

        Assert.That(
            await _sagaHarness.Exists(context.ReservationId, state => state.Cancelling),
            Is.EqualTo(context.ReservationId),
            "Saga not transition to Cancelling");

        return context;
    }

    private async Task<SagaTestContext> CreateSagaInExpiring()
    {
        var context = await CreateSagaInPendingPayment();

        await _harness.Bus.Publish(new BookingPendingPaymentExpiredIntegrationEvent(
            context.ReservationId,
            context.BookingId));

        Assert.That(
            await _sagaHarness.Exists(context.ReservationId, state => state.Expiring),
            Is.EqualTo(context.ReservationId),
            "Saga not transition to Expiring");

        return context;
    }

    private Task PublishFault<TCommand>(TCommand command, string reason)
        where TCommand : class =>
        _harness.Bus.Publish<Fault<TCommand>>(new
        {
            Message = command,
            Timestamp = DateTime.UtcNow,
            Exceptions = new[]
            {
                new
                {
                    ExceptionType = "System.InvalidOperationException",
                    Message = reason
                }
            }
        });

    [Test]
    public async Task ReserveSeatFault_WhenReservingSeat_ShouldCancelBookingAndReleaseReservation()
    {
        var sagaContext = await CreateSagaInReservingSeat();

        await PublishFault(
            new ReserveSeatsCommand(
                sagaContext.ReservationId,
                sagaContext.BookingId,
                ShowtimeId,
                UserId,
                3),
            "Reserve seat failed");

        Assert.That(await _sagaHarness.Consumed.Any<Fault<ReserveSeatsCommand>>(), Is.True);
        Assert.That(await _harness.Published.Any<CancelBookingCommand>(), Is.True);
        Assert.That(await _harness.Published.Any<ReleaseSeatReservationCommand>(), Is.True);
        Assert.That(
            await _sagaHarness.Exists(sagaContext.ReservationId, state => state.Cancelling),
            Is.EqualTo(sagaContext.ReservationId));
        Assert.That(
            _sagaHarness.Sagas.Contains(sagaContext.ReservationId)!.FailureReason,
            Is.EqualTo("ReserveSeatsCommand failed"));
    }

    [Test]
    public async Task SeatHoldExtensionTimedOut_WhenExtendingSeatHold_ShouldCancelBookingAndReleaseSeat()
    {
        var sagaContext = await CreateSagaInExtendingSeatHold();

        await _harness.Bus.Publish(new SeatHoldExtensionTimedOutIntegrationEvent(
            sagaContext.ReservationId,
            sagaContext.BookingId,
            "Seat extension timed out"));

        Assert.That(await _harness.Published.Any<CancelBookingCommand>(), Is.True);
        Assert.That(await _harness.Published.Any<ReleaseSeatReservationCommand>(), Is.True);
        Assert.That(
            await _sagaHarness.Exists(sagaContext.ReservationId, state => state.Cancelling),
            Is.EqualTo(sagaContext.ReservationId));
        Assert.That(
            _sagaHarness.Sagas.Contains(sagaContext.ReservationId)!.FailureReason,
            Is.EqualTo("Seat hold extension timed out"));
    }

    [Test]
    public async Task PaymentFailed_WhenPaymentProcessing_ShouldCancelBookingAndReleaseSeat()
    {
        var sagaContext = await CreateSagaInPaymentProcessing();

        await _harness.Bus.Publish(new PaymentFailedIntegrationEvent(
            sagaContext.ReservationId,
            sagaContext.BookingId,
            "Card declined"));

        Assert.That(await _harness.Published.Any<CancelBookingCommand>(), Is.True);
        Assert.That(await _harness.Published.Any<ReleaseSeatReservationCommand>(), Is.True);
        Assert.That(
            await _sagaHarness.Exists(sagaContext.ReservationId, state => state.Cancelling),
            Is.EqualTo(sagaContext.ReservationId));
        Assert.That(
            _sagaHarness.Sagas.Contains(sagaContext.ReservationId)!.FailureReason,
            Is.EqualTo("Card declined"));
    }

    [Test]
    public async Task PaymentTimedOut_WhenPaymentProcessing_ShouldReconcilePayment()
    {
        var sagaContext = await CreateSagaInPaymentProcessing();

        await _harness.Bus.Publish(new PaymentTimedOutIntegrationEvent(
            sagaContext.ReservationId,
            sagaContext.BookingId));

        Assert.That(
            await _sagaHarness.Exists(sagaContext.ReservationId, state => state.ReconcilingPayment),
            Is.EqualTo(sagaContext.ReservationId));
        Assert.That(
            _sagaHarness.Sagas.Contains(sagaContext.ReservationId)!.FailureReason,
            Is.EqualTo("Payment timed out"));
    }

    [Test]
    public async Task RequestPaymentFault_WhenPaymentProcessing_ShouldCancelBookingAndReleaseSeat()
    {
        var sagaContext = await CreateSagaInPaymentProcessing();

        await PublishFault(
            new RequestPaymentCommand(
                sagaContext.ReservationId,
                sagaContext.BookingId,
                UserId,
                TotalPrice),
            "Payment provider unavailable");

        Assert.That(await _sagaHarness.Consumed.Any<Fault<RequestPaymentCommand>>(), Is.True);
        Assert.That(await _harness.Published.Any<CancelBookingCommand>(), Is.True);
        Assert.That(await _harness.Published.Any<ReleaseSeatReservationCommand>(), Is.True);
        Assert.That(
            await _sagaHarness.Exists(sagaContext.ReservationId, state => state.Cancelling),
            Is.EqualTo(sagaContext.ReservationId));
    }

    [Test]
    public async Task ProviderConfirmedPaid_WhenReconcilingPayment_ShouldConfirmSeats()
    {
        var sagaContext = await CreateSagaInReconcilingPayment();

        await _harness.Bus.Publish(new PaymentProviderConfirmedPaidIntegrationEvent(
            sagaContext.ReservationId,
            sagaContext.BookingId,
            UserId,
            sagaContext.PaymentId,
            TotalPrice));

        Assert.That(
            await _harness.Published.Any<ConfirmSeatReservationCommand>(message =>
                message.Context.Message.ReservationId == sagaContext.ReservationId),
            Is.True);
        Assert.That(
            await _sagaHarness.Exists(sagaContext.ReservationId, state => state.ConfirmingSeats),
            Is.EqualTo(sagaContext.ReservationId));

        var saga = _sagaHarness.Sagas.Contains(sagaContext.ReservationId);
        Assert.That(saga!.PaymentCaptured, Is.True);
        Assert.That(saga.PaymentId, Is.EqualTo(sagaContext.PaymentId));
    }

    [Test]
    public async Task ProviderConfirmedUnpaid_WhenReconcilingPayment_ShouldCancelBookingAndReleaseSeat()
    {
        var sagaContext = await CreateSagaInReconcilingPayment();

        await _harness.Bus.Publish(new PaymentProviderConfirmedUnpaidIntegrationEvent(
            sagaContext.ReservationId,
            sagaContext.BookingId,
            UserId,
            sagaContext.PaymentId,
            TotalPrice));

        Assert.That(await _harness.Published.Any<CancelBookingCommand>(), Is.True);
        Assert.That(await _harness.Published.Any<ReleaseSeatReservationCommand>(), Is.True);
        Assert.That(
            await _sagaHarness.Exists(sagaContext.ReservationId, state => state.Cancelling),
            Is.EqualTo(sagaContext.ReservationId));
    }

    [Test]
    public async Task SeatHoldExpired_WhenReconcilingPayment_ShouldResolveUnknownPayment()
    {
        var sagaContext = await CreateSagaInReconcilingPayment();

        await _harness.Bus.Publish(new SeatHoldDeadlineReachedIntegrationEvent(
            sagaContext.ReservationId,
            sagaContext.BookingId));

        Assert.That(
            await _sagaHarness.Exists(sagaContext.ReservationId, state => state.ResolvingUnknownPayment),
            Is.EqualTo(sagaContext.ReservationId));
    }

    [Test]
    public async Task ProviderConfirmedUnpaid_WhenResolvingUnknownPayment_ShouldCancelBookingAndReleaseSeat()
    {
        var sagaContext = await CreateSagaInResolvingUnknownPayment();

        await _harness.Bus.Publish(new PaymentProviderConfirmedUnpaidIntegrationEvent(
            sagaContext.ReservationId,
            sagaContext.BookingId,
            UserId,
            sagaContext.PaymentId,
            TotalPrice));

        Assert.That(await _harness.Published.Any<CancelBookingCommand>(), Is.True);
        Assert.That(await _harness.Published.Any<ReleaseSeatReservationCommand>(), Is.True);
        Assert.That(
            await _sagaHarness.Exists(sagaContext.ReservationId, state => state.Cancelling),
            Is.EqualTo(sagaContext.ReservationId));
    }

    [Test]
    public async Task PaymentRefunded_WhenRefundingLatePayment_ShouldFinalizeSaga()
    {
        var sagaContext = await CreateSagaInResolvingUnknownPayment();

        await _harness.Bus.Publish(new LatePaymentSuccededIntegrationEvent(
            sagaContext.ReservationId,
            sagaContext.BookingId,
            sagaContext.PaymentId));

        Assert.That(
            await _sagaHarness.Exists(sagaContext.ReservationId, state => state.RefundingLatePayment),
            Is.EqualTo(sagaContext.ReservationId));

        await _harness.Bus.Publish(new PaymentRefundedIntegrationEvent(
            sagaContext.ReservationId,
            sagaContext.BookingId,
            sagaContext.PaymentId));

        Assert.That(await _sagaHarness.NotExists(sagaContext.ReservationId), Is.Null);
    }

    [Test]
    public async Task LatePaymentSucceeded_WhenResolvingUnknownPayment_ShouldRefundLatePayment()
    {
        var sagaContext = await CreateSagaInResolvingUnknownPayment();

        await _harness.Bus.Publish(new LatePaymentSuccededIntegrationEvent(
            sagaContext.ReservationId,
            sagaContext.BookingId,
            sagaContext.PaymentId));

        Assert.That(
            await _sagaHarness.Exists(sagaContext.ReservationId, state => state.RefundingLatePayment),
            Is.EqualTo(sagaContext.ReservationId));
    }

    [Test]
    public async Task SeatConfirmationFailed_WhenConfirmingSeats_ShouldPublishAllCompensationCommands()
    {
        var sagaContext = await CreateSagaInConfirmingSeats();

        await _harness.Bus.Publish(new SeatReservationConfirmationFailedIntegrationEvent(
            sagaContext.ReservationId,
            sagaContext.BookingId,
            "Seat confirmation failed"));

        Assert.That(await _harness.Published.Any<RefundPaymentCommand>(), Is.True);
        Assert.That(await _harness.Published.Any<CancelBookingCommand>(), Is.True);
        Assert.That(await _harness.Published.Any<ReleaseSeatReservationCommand>(), Is.True);
        Assert.That(
            await _sagaHarness.Exists(sagaContext.ReservationId, state => state.Compensating),
            Is.EqualTo(sagaContext.ReservationId));
        Assert.That(
            _sagaHarness.Sagas.Contains(sagaContext.ReservationId)!.FailureReason,
            Is.EqualTo("Seat confirmation failed"));
    }

    [Test]
    public async Task SeatConfirmationTimedOut_WhenConfirmingSeats_ShouldPublishAllCompensationCommands()
    {
        var sagaContext = await CreateSagaInConfirmingSeats();

        await _harness.Bus.Publish(new SeatReservationConfirmationTimedOutIntegrationEvent(
            sagaContext.ReservationId,
            sagaContext.BookingId));

        Assert.That(await _harness.Published.Any<RefundPaymentCommand>(), Is.True);
        Assert.That(await _harness.Published.Any<CancelBookingCommand>(), Is.True);
        Assert.That(await _harness.Published.Any<ReleaseSeatReservationCommand>(), Is.True);
        Assert.That(
            await _sagaHarness.Exists(sagaContext.ReservationId, state => state.Compensating),
            Is.EqualTo(sagaContext.ReservationId));
        Assert.That(
            _sagaHarness.Sagas.Contains(sagaContext.ReservationId)!.FailureReason,
            Is.EqualTo("Seat confirmation timed out"));
    }

    [Test]
    public async Task ConfirmSeatReservationFault_WhenConfirmingSeats_ShouldPublishAllCompensationCommands()
    {
        var sagaContext = await CreateSagaInConfirmingSeats();

        await PublishFault(
            new ConfirmSeatReservationCommand(
                sagaContext.ReservationId,
                sagaContext.BookingId,
                ShowtimeId,
                UserId,
                3),
            "Confirm seat command failed");

        Assert.That(await _harness.Published.Any<RefundPaymentCommand>(), Is.True);
        Assert.That(await _harness.Published.Any<CancelBookingCommand>(), Is.True);
        Assert.That(await _harness.Published.Any<ReleaseSeatReservationCommand>(), Is.True);
        Assert.That(
            await _sagaHarness.Exists(sagaContext.ReservationId, state => state.Compensating),
            Is.EqualTo(sagaContext.ReservationId));
    }

    [Test]
    public async Task MarkBookingPaidFault_WhenCompletingBooking_ShouldStartCompensation()
    {
        var sagaContext = await CreateSagaInCompletingBooking();

        await PublishFault(
            new MarkBookingPaidCommand(
                sagaContext.ReservationId,
                sagaContext.BookingId,
                sagaContext.PaymentId),
            "Mark booking paid failed");

        Assert.That(await _harness.Published.Any<RefundPaymentCommand>(), Is.True);
        Assert.That(await _harness.Published.Any<CancelBookingCommand>(), Is.True);
        Assert.That(await _harness.Published.Any<ReleaseSeatReservationCommand>(), Is.True);
        Assert.That(
            await _sagaHarness.Exists(sagaContext.ReservationId, state => state.Compensating),
            Is.EqualTo(sagaContext.ReservationId));
    }

    [TestCase("BookingExpired")]
    [TestCase("ReservationReleased")]
    public async Task Expiring_WhenBothAcknowledgementsReceived_ShouldFinalizeSaga(string lastEvent)
    {
        var sagaContext = await CreateSagaInExpiring();

        if (lastEvent == "BookingExpired")
        {
            await _harness.Bus.Publish(new SeatReservationReleasedIntegrationEvent(
                sagaContext.ReservationId,
                sagaContext.BookingId));
            Assert.That(
                await _sagaHarness.Exists(sagaContext.ReservationId, state => state.Expiring),
                Is.EqualTo(sagaContext.ReservationId));
            await _harness.Bus.Publish(new BookingStatusChangedToExpiredIntegrationEvent(
                sagaContext.ReservationId,
                sagaContext.BookingId));
        }
        else
        {
            await _harness.Bus.Publish(new BookingStatusChangedToExpiredIntegrationEvent(
                sagaContext.ReservationId,
                sagaContext.BookingId));
            Assert.That(
                await _sagaHarness.Exists(sagaContext.ReservationId, state => state.Expiring),
                Is.EqualTo(sagaContext.ReservationId));
            await _harness.Bus.Publish(new SeatReservationReleasedIntegrationEvent(
                sagaContext.ReservationId,
                sagaContext.BookingId));
        }

        Assert.That(await _sagaHarness.NotExists(sagaContext.ReservationId), Is.Null);
    }

    [Test]
    public async Task MarkBookingExpiredFault_WhenExpiring_ShouldReleaseSeatAndCancelBooking()
    {
        var sagaContext = await CreateSagaInExpiring();

        await PublishFault(
            new MarkBookingExpiredCommand(
                sagaContext.ReservationId,
                sagaContext.BookingId),
            "Mark booking expired failed");

        Assert.That(await _harness.Published.Any<ReleaseSeatReservationCommand>(), Is.True);
        Assert.That(
            await _sagaHarness.Exists(sagaContext.ReservationId, state => state.Cancelling),
            Is.EqualTo(sagaContext.ReservationId));
        Assert.That(
            _sagaHarness.Sagas.Contains(sagaContext.ReservationId)!.FailureReason,
            Is.EqualTo("MarkBookingExpiredCommand failed"));
    }

    [TestCase("PaymentRefunded")]
    [TestCase("BookingCancelled")]
    [TestCase("ReservationReleased")]
    public async Task Compensating_WhenAllAcknowledgementsReceived_ShouldFinalizeSaga(string lastEvent)
    {
        var sagaContext = await CreateSagaInCompensating();

        if (lastEvent != "PaymentRefunded")
            await _harness.Bus.Publish(new PaymentRefundedIntegrationEvent(
                sagaContext.ReservationId,
                sagaContext.BookingId,
                sagaContext.PaymentId));

        if (lastEvent != "BookingCancelled")
            await _harness.Bus.Publish(new BookingStatusChangedToCancelledIntegrationEvent(
                sagaContext.ReservationId,
                sagaContext.BookingId));

        if (lastEvent != "ReservationReleased")
            await _harness.Bus.Publish(new SeatReservationReleasedIntegrationEvent(
                sagaContext.ReservationId,
                sagaContext.BookingId));

        Assert.That(
            await _sagaHarness.Exists(sagaContext.ReservationId, state => state.Compensating),
            Is.EqualTo(sagaContext.ReservationId));

        switch (lastEvent)
        {
            case "PaymentRefunded":
                await _harness.Bus.Publish(new PaymentRefundedIntegrationEvent(
                    sagaContext.ReservationId,
                    sagaContext.BookingId,
                    sagaContext.PaymentId));
                break;
            case "BookingCancelled":
                await _harness.Bus.Publish(new BookingStatusChangedToCancelledIntegrationEvent(
                    sagaContext.ReservationId,
                    sagaContext.BookingId));
                break;
            default:
                await _harness.Bus.Publish(new SeatReservationReleasedIntegrationEvent(
                    sagaContext.ReservationId,
                    sagaContext.BookingId));
                break;
        }

        Assert.That(await _sagaHarness.NotExists(sagaContext.ReservationId), Is.Null);
    }

    [Test]
    public async Task RefundPaymentFault_WhenCompensating_ShouldTransitionToCompensationFailed()
    {
        var sagaContext = await CreateSagaInCompensating();

        await PublishFault(
            new RefundPaymentCommand(
                sagaContext.ReservationId,
                sagaContext.BookingId,
                sagaContext.PaymentId,
                TotalPrice,
                "Compensation"),
            "Refund failed");

        Assert.That(
            await _sagaHarness.Exists(sagaContext.ReservationId, state => state.CompensationFailed),
            Is.EqualTo(sagaContext.ReservationId));
    }

    [Test]
    public async Task MarkBookingPaidFault_WhenCompensating_ShouldTransitionToCompensationFailed()
    {
        var sagaContext = await CreateSagaInCompensating();

        await PublishFault(
            new MarkBookingPaidCommand(
                sagaContext.ReservationId,
                sagaContext.BookingId,
                sagaContext.PaymentId),
            "Mark booking paid compensation failed");

        Assert.That(
            await _sagaHarness.Exists(sagaContext.ReservationId, state => state.CompensationFailed),
            Is.EqualTo(sagaContext.ReservationId));
    }

    [Test]
    public async Task ReleaseReservationFault_WhenCompensating_ShouldTransitionToCompensationFailed()
    {
        var sagaContext = await CreateSagaInCompensating();

        await PublishFault(
            new ReleaseSeatReservationCommand(
                sagaContext.ReservationId,
                sagaContext.BookingId,
                ShowtimeId,
                UserId),
            "Release reservation failed");

        Assert.That(
            await _sagaHarness.Exists(sagaContext.ReservationId, state => state.CompensationFailed),
            Is.EqualTo(sagaContext.ReservationId));
    }

    [TestCase("BookingCancelled")]
    [TestCase("ReservationReleased")]
    public async Task Cancelling_WhenBothAcknowledgementsReceived_ShouldFinalizeSaga(string lastEvent)
    {
        var sagaContext = await CreateSagaInCancelling();

        if (lastEvent == "BookingCancelled")
        {
            await _harness.Bus.Publish(new SeatReservationReleasedIntegrationEvent(
                sagaContext.ReservationId,
                sagaContext.BookingId));
            Assert.That(
                await _sagaHarness.Exists(sagaContext.ReservationId, state => state.Cancelling),
                Is.EqualTo(sagaContext.ReservationId));
            await _harness.Bus.Publish(new BookingStatusChangedToCancelledIntegrationEvent(
                sagaContext.ReservationId,
                sagaContext.BookingId));
        }
        else
        {
            await _harness.Bus.Publish(new BookingStatusChangedToCancelledIntegrationEvent(
                sagaContext.ReservationId,
                sagaContext.BookingId));
            Assert.That(
                await _sagaHarness.Exists(sagaContext.ReservationId, state => state.Cancelling),
                Is.EqualTo(sagaContext.ReservationId));
            await _harness.Bus.Publish(new SeatReservationReleasedIntegrationEvent(
                sagaContext.ReservationId,
                sagaContext.BookingId));
        }

        Assert.That(await _sagaHarness.NotExists(sagaContext.ReservationId), Is.Null);
    }

    [Test]
    public async Task CancelBookingFault_WhenCancelling_ShouldTransitionToCancellationFailed()
    {
        var sagaContext = await CreateSagaInCancelling();

        await PublishFault(
            new CancelBookingCommand(
                sagaContext.ReservationId,
                sagaContext.BookingId,
                "Cancellation"),
            "Cancel booking failed");

        Assert.That(
            await _sagaHarness.Exists(sagaContext.ReservationId, state => state.CancellationFailed),
            Is.EqualTo(sagaContext.ReservationId));
    }

    [Test]
    public async Task ReleaseReservationFault_WhenCancelling_ShouldTransitionToCancellationFailed()
    {
        var sagaContext = await CreateSagaInCancelling();

        await PublishFault(
            new ReleaseSeatReservationCommand(
                sagaContext.ReservationId,
                sagaContext.BookingId,
                ShowtimeId,
                UserId),
            "Release reservation failed");

        Assert.That(
            await _sagaHarness.Exists(sagaContext.ReservationId, state => state.CancellationFailed),
            Is.EqualTo(sagaContext.ReservationId));
    }
}
