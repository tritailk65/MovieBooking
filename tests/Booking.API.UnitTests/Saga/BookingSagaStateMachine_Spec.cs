namespace Booking.API.UnitTests.Saga;

[TestFixture]
public class BookingSagaStateMachineSpec
{
    private ServiceProvider _provider = null!;
    private ITestHarness _harness = null!;
    private QuartzTimeAdjustment _adjustment;
    private ISagaStateMachineTestHarness<BookingStateMachine, BookingSaga> _sagaHarness = null!;

    #region Mock data
    private const int ShowtimeId = 1;
    private const string UserId = "2779fb04-052e-49c1-8ce0-c200d8e06b6f";
    private const decimal TotalPrice = 180_000m;
    private sealed record SagaTestContext(
        Guid ReservationId,
        int BookingId,
        string PaymentId);
    #endregion

    #region Helper function
    private async Task<SagaTestContext> CreateSagaInReservingSeat()
    {
        var context = new SagaTestContext(
            ReservationId: NewId.NextGuid(),
            BookingId: Random.Shared.Next(1, int.MaxValue),
            PaymentId: NewId.NextGuid().ToString());

        await _harness.Bus.Publish(
            new BookingStatusChangedToSubmittedIntegrationEvent(
                context.ReservationId,
                context.BookingId,
                ShowtimeId,
                UserId,
                ["A1", "A2"],
                TotalPrice,
                ReservationVersion: 3,
                PreparedUntil: DateTime.UtcNow.AddMinutes(5)));

        Assert.That(await _sagaHarness.Exists(context.ReservationId,state => state.ReservingSeat),Is.EqualTo(context.ReservationId));
        return context;
    }

    private async Task<SagaTestContext> CreateSagaInPendingPayment(SagaTestContext context)
    {
        Assert.That(await _harness.Consumed.Any<BookingStatusChangedToSubmittedIntegrationEvent>(), Is.True, "Message BookingStatusChangedToSubmittedIntegrationEvent not consumed");

        Assert.That(await _sagaHarness.Consumed.Any<BookingStatusChangedToSubmittedIntegrationEvent>(), "Message not consumed by saga");   
        Assert.That(await _sagaHarness.Exists(context.ReservationId, state => state.ReservingSeat), Is.EqualTo(context.ReservationId), "Saga not transition to ReservingSeat");

        await _harness.Bus.Publish(new SeatReservationHeldIntegrationEvent(context.ReservationId, context.BookingId));
        Assert.That(await _sagaHarness.Exists(context.ReservationId, state => state.PendingPayment), Is.EqualTo(context.ReservationId), "Saga not transition to PendingPayment");

        return context;
    }

    private async Task<SagaTestContext> CreateSagaInExtendingSeatHold(SagaTestContext context)
    {
        await _harness.Bus.Publish(new PaymentRequestedIntegrationEvent(context.ReservationId, context.BookingId, UserId, TotalPrice));
        Assert.That(await _sagaHarness.Consumed.Any<PaymentRequestedIntegrationEvent>(message =>
                message.Context.Message.ReservationId == context.ReservationId),
            Is.True);

        Assert.That(await _sagaHarness.Exists(context.ReservationId, state => state.ExtendingSeatHold), Is.EqualTo(context.ReservationId), "Saga not transition to ExtendingSeatHold");
        return context;
    }
    #endregion

    #region Setup and TearDown
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
        _adjustment = new QuartzTimeAdjustment(_provider);
    }

    [TearDown]
    public async Task Teardown()
    {
        await _harness.Stop();
        await _provider.DisposeAsync();
        _adjustment.Dispose();
    }
    #endregion

    [Test]
    public async Task BookingSubmitted_ShouldCreateSaga()
    {
        var sagaContext = await CreateSagaInReservingSeat();

        Assert.That(await _harness.Consumed.Any<BookingStatusChangedToSubmittedIntegrationEvent>(), Is.True, "Message not consumed");
        Assert.That(await _sagaHarness.Consumed.Any<BookingStatusChangedToSubmittedIntegrationEvent>(), "Message not consumed by saga");

        // Assert.That(
        //     await _harness.Published.Any<ReserveSeatsCommand>(message =>
        //         message.Context.Message.ReservationId == ReservationId &&
        //         message.Context.Message.BookingId == BookingId &&
        //         message.Context.Message.ReservationVersion == 3),
        //     Is.True);

        var saga = _sagaHarness.Sagas.Contains(sagaContext.ReservationId);
        Assert.That(saga, Is.Not.Null);

        Assert.Multiple(() =>
        {
            Assert.That(saga!.BookingId, Is.EqualTo(sagaContext.BookingId));
            Assert.That(saga.ShowtimeId, Is.EqualTo(ShowtimeId));
            Assert.That(saga.UserId, Is.EqualTo(UserId));
            Assert.That(saga.Seats, Is.EqualTo(new[] { "A1", "A2" }));
            Assert.That(saga.ReservationVersion, Is.EqualTo(3));
        });
        
        var instance = _sagaHarness.Created.ContainsInState(sagaContext.ReservationId, _sagaHarness.StateMachine, _sagaHarness.StateMachine.ReservingSeat);
        Assert.That(instance, Is.Not.Null, "Saga instance not found");

        Guid? existsId = await _sagaHarness.Exists(sagaContext.ReservationId, x => x.ReservingSeat);
        Assert.That(existsId.HasValue, Is.True, "Saga did not exist");
    }

    [Test]
    public async Task SeatReservationHeld_ShouldCreateBookingSchedule()
    {
        var sagaContext = await CreateSagaInReservingSeat();

        Assert.That(await _harness.Consumed.Any<BookingStatusChangedToSubmittedIntegrationEvent>(), Is.True, "Message not consumed");
        Assert.That(await _sagaHarness.Consumed.Any<BookingStatusChangedToSubmittedIntegrationEvent>(), "Message not consumed by saga");

        Assert.That(await _sagaHarness.Exists(sagaContext.ReservationId, state => state.ReservingSeat), Is.EqualTo(sagaContext.ReservationId));

        await _harness.Bus.Publish(new SeatReservationHeldIntegrationEvent(sagaContext.ReservationId, sagaContext.BookingId));

        Assert.That(await _sagaHarness.Consumed.Any<SeatReservationHeldIntegrationEvent>(message =>
                    message.Context.Message.ReservationId == sagaContext.ReservationId &&
                    message.Context.Message.BookingId == sagaContext.BookingId),
                Is.True);

        Assert.That(await _sagaHarness.Exists(sagaContext.ReservationId, state => state.PendingPayment), Is.EqualTo(sagaContext.ReservationId));

        var saga = _sagaHarness.Sagas.Contains(sagaContext.ReservationId);

        Assert.That(saga, Is.Not.Null);
        Assert.That(saga!.PendingPaymentExpirationTokenId,Is.Not.Null);

    }

    [Test]
    public async Task SeatReservationFailed_ShouldCancelBookingAndReleaseReservation()
    {
        var sagaContext = await CreateSagaInReservingSeat();

        Assert.That(await _harness.Consumed.Any<BookingStatusChangedToSubmittedIntegrationEvent>(), Is.True, "Message not consumed");
        Assert.That(await _sagaHarness.Consumed.Any<BookingStatusChangedToSubmittedIntegrationEvent>(), "Message not consumed by saga");

        await _harness.Bus.Publish(new SeatReservationFailedIntegrationEvent(sagaContext.ReservationId, sagaContext.BookingId,  "Cannot lock seat"));

        Assert.That(await _sagaHarness.Consumed.Any<SeatReservationFailedIntegrationEvent>(message =>
                    message.Context.Message.ReservationId == sagaContext.ReservationId &&
                    message.Context.Message.BookingId == sagaContext.BookingId),
                Is.True);

        Assert.That(await _harness.Published.Any<CancelBookingCommand>(), Is.True);
        Assert.That(await _harness.Published.Any<ReleaseSeatReservationCommand>(), Is.True);

        var instance = _sagaHarness.Created.ContainsInState(sagaContext.ReservationId, _sagaHarness.StateMachine, _sagaHarness.StateMachine.Cancelling);
        Assert.That(instance, Is.Not.Null, "Saga instance not found");

        Guid? existsId = await _sagaHarness.Exists(sagaContext.ReservationId, x => x.Cancelling);
        Assert.That(existsId.HasValue, Is.True, "Saga did not exist");
    }

    [Test]
    public async Task SeatReservationTimedOut_ShouldCancelBookingAndReleaseReservation()
    {
        var sagaContext = await CreateSagaInReservingSeat();

        Assert.That(await _harness.Consumed.Any<BookingStatusChangedToSubmittedIntegrationEvent>(), Is.True, "Message not consumed");
        Assert.That(await _sagaHarness.Consumed.Any<BookingStatusChangedToSubmittedIntegrationEvent>(), "Message not consumed by saga");
        
        await _harness.Bus.Publish(new SeatReservationTimedOutIntegrationEvent(sagaContext.ReservationId, sagaContext.BookingId));

        Assert.That(await _sagaHarness.Consumed.Any<SeatReservationTimedOutIntegrationEvent>(message =>
                    message.Context.Message.ReservationId == sagaContext.ReservationId &&
                    message.Context.Message.BookingId == sagaContext.BookingId),
                Is.True);

        Assert.That(await _harness.Published.Any<CancelBookingCommand>(), Is.True);
        Assert.That(await _harness.Published.Any<ReleaseSeatReservationCommand>(), Is.True);

        Guid? existsId = await _sagaHarness.Exists(sagaContext.ReservationId, x => x.Cancelling);
        Assert.That(existsId.HasValue, Is.True, "Saga did not exist");
    }

    [Test]
    public async Task PaymentRequested_WhenPendingPayment_ShouldCancelBookingExpirationSchedule()
    {
        var sagaContext = await CreateSagaInReservingSeat();
        await CreateSagaInPendingPayment(sagaContext);

        var sagaBefore = _sagaHarness.Sagas.Contains(sagaContext.ReservationId);

        await _harness.Bus.Publish(new PaymentRequestedIntegrationEvent(sagaContext.ReservationId, sagaContext.BookingId, UserId, TotalPrice));
        Assert.That(await _sagaHarness.Consumed.Any<PaymentRequestedIntegrationEvent>(message =>
                message.Context.Message.ReservationId == sagaContext.ReservationId),
            Is.True);

        Assert.That(await _harness.Published.Any<ExtendSeatHoldCommand>(), Is.True);   

        Assert.That(await _sagaHarness.Exists(sagaContext.ReservationId, state => state.ExtendingSeatHold), Is.EqualTo(sagaContext.ReservationId));

        Assert.That(_sagaHarness.Sagas.Contains(sagaContext.ReservationId)!.PendingPaymentExpirationTokenId, Is.Null,"Schedule token was not cleared from saga");

    }

    [Test]
    public async Task BookingScheduleExpired_WhenPendingPayment_ShouldReleaseReservationAndMarkBookingExpired()
    {
        var sagaContext = await CreateSagaInReservingSeat();
        await CreateSagaInPendingPayment(sagaContext);

        using var adjustment = new QuartzTimeAdjustment(_provider);
        await adjustment.AdvanceTime(TimeSpan.FromMinutes(10));

        Assert.That(await _harness.Published.Any<MarkBookingExpiredCommand>(message =>
                message.Context.Message.ReservationId == sagaContext.ReservationId &&
                message.Context.Message.BookingId == sagaContext.BookingId),
            Is.True);

        Assert.That(await _sagaHarness.Exists(sagaContext.ReservationId, state => state.Expiring), Is.EqualTo(sagaContext.ReservationId), "Saga not transition to Expiring");
    }

    [Test]
    public async Task BookingCancellationRequest_WhenPendingPayment_ShouldCancelBookingAndReleaseSeat()
    {
        var sagaContext = await CreateSagaInReservingSeat();
        await CreateSagaInPendingPayment(sagaContext);

        await _harness.Bus.Publish(new BookingCancellationRequestedIntegrationEvent(sagaContext.ReservationId, sagaContext.BookingId, UserId, ShowtimeId));

        Assert.That(await _sagaHarness.Consumed.Any<BookingCancellationRequestedIntegrationEvent>(message =>
                    message.Context.Message.ReservationId == sagaContext.ReservationId &&
                    message.Context.Message.BookingId == sagaContext.BookingId),
                Is.True);

        Assert.That(await _harness.Published.Any<CancelBookingCommand>(), Is.True);
        Assert.That(await _harness.Published.Any<ReleaseSeatReservationCommand>(), Is.True);

        Assert.That(await _sagaHarness.Exists(sagaContext.ReservationId, state => state.Cancelling), Is.EqualTo(sagaContext.ReservationId), "Saga not transition to Expiring");

        Assert.That(_sagaHarness.Sagas.Contains(sagaContext.ReservationId)!.PendingPaymentExpirationTokenId, Is.Null, "Schedule token was not cleared from saga");
    }

    [Test]
    public async Task SeatHoldExtended_WhenExtendingSeatHold_ShouldPublishRequestPaymentCommand()
    {
        var sagaContext = await CreateSagaInReservingSeat();
        await CreateSagaInPendingPayment(sagaContext);
   
        await _harness.Bus.Publish(new PaymentRequestedIntegrationEvent(sagaContext.ReservationId, sagaContext.BookingId, UserId, TotalPrice));
        Assert.That(await _sagaHarness.Consumed.Any<PaymentRequestedIntegrationEvent>(message =>
                message.Context.Message.ReservationId == sagaContext.ReservationId),
            Is.True);

        Assert.That(await _sagaHarness.Exists(sagaContext.ReservationId, state => state.ExtendingSeatHold), Is.EqualTo(sagaContext.ReservationId), "Saga not transition to ExtendingSeatHold");
        
        await _harness.Bus.Publish(new SeatHoldExtendedIntegrationEvent(sagaContext.ReservationId, sagaContext.BookingId));
        Assert.That(await _sagaHarness.Consumed.Any<SeatHoldExtendedIntegrationEvent>(message =>
                message.Context.Message.ReservationId == sagaContext.ReservationId &&
                message.Context.Message.BookingId == sagaContext.BookingId),
            Is.True);

        Assert.That(await _harness.Published.Any<RequestPaymentCommand>(), Is.True,  "Saga not publish RequestPaymentCommand");

        Assert.That(await _sagaHarness.Exists(sagaContext.ReservationId, state => state.PaymentProcessing), Is.EqualTo(sagaContext.ReservationId), "Saga not transition to PaymentProcessing");
    }

    [Test]
    public async Task SeatHoldExtensionFailed_WhenExtendingSeatHold_ShouldCancelBookingAndReleaseSeat()
    {
        var sagaContext = await CreateSagaInReservingSeat();
        await CreateSagaInPendingPayment(sagaContext);
        await CreateSagaInExtendingSeatHold(sagaContext);

        await _harness.Bus.Publish(new SeatHoldExtensionFailedIntegrationEvent(sagaContext.ReservationId, sagaContext.BookingId, "Cannot extend seat hold deadline"));
        Assert.That(await _sagaHarness.Consumed.Any<SeatHoldExtensionFailedIntegrationEvent>(message =>
                message.Context.Message.ReservationId == sagaContext.ReservationId &&
                message.Context.Message.BookingId == sagaContext.BookingId),
            Is.True);


        Assert.That(await _harness.Published.Any<CancelBookingCommand>(), Is.True);
        Assert.That(await _harness.Published.Any<ReleaseSeatReservationCommand>(), Is.True);
        Assert.That(await _sagaHarness.Exists(sagaContext.ReservationId, state => state.Cancelling), Is.EqualTo(sagaContext.ReservationId), "Saga not transition to Expiring");
    }

    [Test]
    public async Task SeatHoldExtensionFault_WhenExtendingSeatHold_ShouldCancelBookingAndReleaseSeat()
    {
        var sagaContext = await CreateSagaInReservingSeat();
        await CreateSagaInPendingPayment(sagaContext);
        await CreateSagaInExtendingSeatHold(sagaContext);

        Assert.That((await _sagaHarness.Exists(sagaContext.ReservationId, x => x.ExtendingSeatHold)).HasValue, Is.True, "Saga must be in ExtendingSeatHold state first");

        await _harness.Bus.Publish<Fault<ExtendSeatHoldCommand>>(new
        {
            Message = new ExtendSeatHoldCommand(sagaContext.ReservationId, sagaContext.BookingId, ShowtimeId, UserId),    
            Timestamp = DateTime.UtcNow,
            Exceptions = new[] 
            { 
                new 
                { 
                    ExceptionType = "System.TimeoutException", 
                    Message = "Simulated DB Timeout during ExtendSeatHold" 
                } 
            }
        });

        Assert.That(await _sagaHarness.Consumed.Any<Fault<ExtendSeatHoldCommand>>(), Is.True, "Saga did not consume the Fault event");

        Assert.That(await _sagaHarness.Exists(sagaContext.ReservationId, state => state.Cancelling), Is.EqualTo(sagaContext.ReservationId), "Saga not transition to PaymentProcessing");
    }

    [Test]
    public async Task PaymentSucceeded_WhenPaymentProcessing_ShouldConfirmSeatReservation()
    {
        var sagaContext = await CreateSagaInReservingSeat();
        await CreateSagaInPendingPayment(sagaContext);
        await CreateSagaInExtendingSeatHold(sagaContext);

        await _harness.Bus.Publish(new SeatHoldExtendedIntegrationEvent(sagaContext.ReservationId, sagaContext.BookingId));
        Assert.That(await _sagaHarness.Consumed.Any<SeatHoldExtendedIntegrationEvent>(message =>
                message.Context.Message.ReservationId == sagaContext.ReservationId &&
                message.Context.Message.BookingId == sagaContext.BookingId),
            Is.True);
        Assert.That(await _sagaHarness.Exists(sagaContext.ReservationId, state => state.PaymentProcessing), Is.EqualTo(sagaContext.ReservationId), "Saga not transition to PaymentProcessing");

        await _harness.Bus.Publish(new PaymentSucceededIntegrationEvent(sagaContext.ReservationId, sagaContext.BookingId, sagaContext.PaymentId));

        Assert.That(await _harness.Published.Any<ConfirmSeatReservationCommand>(), Is.True,  "Saga not publish ConfirmSeatReservationCommand");
        Assert.That(await _sagaHarness.Exists(sagaContext.ReservationId, state => state.ConfirmingSeats), Is.EqualTo(sagaContext.ReservationId), "Saga not transition to ConfirmingSeats");
    }

    [Test]
    public async Task SeatsConfirmed_WhenConfirmingSeats_ShouldMarkBookingPaid()
    {
        var sagaContext = await CreateSagaInReservingSeat();
        await CreateSagaInPendingPayment(sagaContext);
        await CreateSagaInExtendingSeatHold(sagaContext);

        await _harness.Bus.Publish(new SeatHoldExtendedIntegrationEvent(sagaContext.ReservationId, sagaContext.BookingId));
        Assert.That(await _sagaHarness.Consumed.Any<SeatHoldExtendedIntegrationEvent>(message =>
                message.Context.Message.ReservationId == sagaContext.ReservationId &&
                message.Context.Message.BookingId == sagaContext.BookingId),
            Is.True);
        Assert.That(await _sagaHarness.Exists(sagaContext.ReservationId, state => state.PaymentProcessing), Is.EqualTo(sagaContext.ReservationId), "Saga not transition to PaymentProcessing");

        await _harness.Bus.Publish(new PaymentSucceededIntegrationEvent(sagaContext.ReservationId, sagaContext.BookingId, sagaContext.PaymentId));
        Assert.That(await _sagaHarness.Exists(sagaContext.ReservationId, state => state.ConfirmingSeats), Is.EqualTo(sagaContext.ReservationId), "Saga not transition to ConfirmingSeats");

        await _harness.Bus.Publish(new SeatReservationConfirmedIntegrationEvent(sagaContext.ReservationId, sagaContext.BookingId));
        Assert.That(await _sagaHarness.Exists(sagaContext.ReservationId, state => state.CompletingBooking), Is.EqualTo(sagaContext.ReservationId), "Saga not transition to CompletingBooking");
    }
    
    [Test]
    public async Task BookingPaid_WhenCompletingBooking_ShouldFinalizeSaga()
    {
        var sagaContext = await CreateSagaInReservingSeat();
        await CreateSagaInPendingPayment(sagaContext);
        await CreateSagaInExtendingSeatHold(sagaContext);

        await _harness.Bus.Publish(new SeatHoldExtendedIntegrationEvent(sagaContext.ReservationId, sagaContext.BookingId));
        Assert.That(await _sagaHarness.Consumed.Any<SeatHoldExtendedIntegrationEvent>(message =>
                message.Context.Message.ReservationId == sagaContext.ReservationId &&
                message.Context.Message.BookingId == sagaContext.BookingId),
            Is.True);
        Assert.That(await _sagaHarness.Exists(sagaContext.ReservationId, state => state.PaymentProcessing), Is.EqualTo(sagaContext.ReservationId), "Saga not transition to PaymentProcessing");

        await _harness.Bus.Publish(new PaymentSucceededIntegrationEvent(sagaContext.ReservationId, sagaContext.BookingId, sagaContext.PaymentId));
        Assert.That(await _sagaHarness.Exists(sagaContext.ReservationId, state => state.ConfirmingSeats), Is.EqualTo(sagaContext.ReservationId), "Saga not transition to ConfirmingSeats");

        await _harness.Bus.Publish(new SeatReservationConfirmedIntegrationEvent(sagaContext.ReservationId, sagaContext.BookingId));
        Assert.That(await _sagaHarness.Exists(sagaContext.ReservationId, state => state.CompletingBooking), Is.EqualTo(sagaContext.ReservationId), "Saga not transition to CompletingBooking");

        await _harness.Bus.Publish(new BookingStatusChangedToPaidIntegrationEvent(sagaContext.ReservationId, sagaContext.BookingId));
        
        Assert.That(await _sagaHarness.NotExists(sagaContext.ReservationId), Is.Null, "Saga was not removed after finalization");
    }

}


