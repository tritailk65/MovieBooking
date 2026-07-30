using SagaOrchestration.Contract;

namespace SagaOrchestration;

public sealed class BookingStateMachine : MassTransitStateMachine<BookingSaga>
{
    private readonly ILogger<BookingStateMachine> _logger;

    public State PaymentPending { get; private set; } = null!;
    public State ConfirmingSeats { get; private set; } = null!;
    public State CompletingBooking { get; private set; } = null!;
    public State Cancelling { get; private set; } = null!;
    public State Compensating { get; private set; } = null!;
    public State CompletionFailed { get; private set; } = null!;
    public State CancellationFailed { get; private set; } = null!;
    public State CompensationFailed { get; private set; } = null!;

    public Event<BookingSubmittedIntegrationEvent> BookingSubmitted { get; private set; } = null!;
    public Event<PaymentSucceededIntegrationEvent> PaymentSucceeded { get; private set; } = null!;
    public Event<PaymentFailedIntegrationEvent> PaymentFailed { get; private set; } = null!;
    public Event<PaymentTimedOutIntegrationEvent> PaymentTimedOut { get; private set; } = null!;
    public Event<SeatReservationConfirmedIntegrationEvent> SeatsConfirmed { get; private set; } = null!;
    public Event<SeatReservationConfirmationFailedIntegrationEvent> SeatConfirmationFailed { get; private set; } = null!;
    public Event<SeatReservationConfirmationTimedOutIntegrationEvent> SeatConfirmationTimedOut { get; private set; } = null!;
    public Event<BookingMarkedPaidIntegrationEvent> BookingMarkedPaid { get; private set; } = null!;
    public Event<BookingCancelledIntegrationEvent> BookingCancelled { get; private set; } = null!;
    public Event<SeatReservationReleasedIntegrationEvent> ReservationReleased { get; private set; } = null!;
    public Event<PaymentRefundedIntegrationEvent> PaymentRefunded { get; private set; } = null!;

    public Event<Fault<RequestPaymentCommand>> RequestPaymentFaulted { get; private set; } = null!;
    public Event<Fault<ConfirmSeatReservationCommand>> ConfirmSeatsFaulted { get; private set; } = null!;
    public Event<Fault<MarkBookingPaidCommand>> MarkBookingPaidFaulted { get; private set; } = null!;
    public Event<Fault<CancelBookingCommand>> CancelBookingFaulted { get; private set; } = null!;
    public Event<Fault<ReleaseSeatReservationCommand>> ReleaseReservationFaulted { get; private set; } = null!;
    public Event<Fault<RefundPaymentCommand>> RefundPaymentFaulted { get; private set; } = null!;

    public BookingStateMachine(ILogger<BookingStateMachine> logger)
    {
        _logger = logger;

        InstanceState(x => x.CurrentState);

        ConfigureCorrelations();

        Initially(
            When(BookingSubmitted)
                .Then(InitializeSaga)
                .Then(LogSagaState)
                .Publish(context => new RequestPaymentCommand(
                    context.Saga.ReservationId,
                    context.Saga.BookingId,
                    context.Saga.UserId,
                    context.Saga.TotalPrice))
                .TransitionTo(PaymentPending));

        During(PaymentPending,
            When(PaymentSucceeded)
                .Then(context =>
                {
                    context.Saga.PaymentId = context.Message.PaymentId;
                    context.Saga.PaymentCaptured = true;
                })
                .Then(LogSagaState)
                .Publish(context => new ConfirmSeatReservationCommand(
                    context.Saga.ReservationId,
                    context.Saga.BookingId,
                    context.Saga.ShowtimeId,
                    context.Saga.UserId,
                    context.Saga.ReservationVersion))
                .TransitionTo(ConfirmingSeats),

            When(PaymentFailed)
                .Then(context => context.Saga.FailureReason = context.Message.Reason)
                .Then(LogSagaState)
                .Publish(CreateCancelBookingCommand)
                .Publish(CreateReleaseReservationCommand)
                .TransitionTo(Cancelling),

            When(PaymentTimedOut)
                .Then(context => context.Saga.FailureReason = "Payment timed out")
                .Then(LogSagaState)
                .Publish(CreateCancelBookingCommand)
                .Publish(CreateReleaseReservationCommand)
                .TransitionTo(Cancelling),

            When(RequestPaymentFaulted)
                .Then(context => context.Saga.FailureReason = FirstFaultMessage(context.Message))
                .Then(LogSagaState)
                .Publish(CreateCancelBookingCommand)
                .Publish(CreateReleaseReservationCommand)
                .TransitionTo(Cancelling));

        During(ConfirmingSeats,
            When(SeatsConfirmed)
                .Then(context => context.Saga.SeatsConfirmed = true)
                .Then(LogSagaState)
                .Publish(context => new MarkBookingPaidCommand(
                    context.Saga.ReservationId,
                    context.Saga.BookingId,
                    context.Saga.PaymentId))
                .TransitionTo(CompletingBooking),

            When(SeatConfirmationFailed)
                .Then(context => context.Saga.FailureReason = context.Message.Reason)
                .Then(LogSagaState)
                .Publish(CreateRefundPaymentCommand)
                .Publish(CreateCancelBookingCommand)
                .Publish(CreateReleaseReservationCommand)
                .TransitionTo(Compensating),

            When(SeatConfirmationTimedOut)
                .Then(context => context.Saga.FailureReason = "Seat confirmation timed out")
                .Then(LogSagaState)
                .Publish(CreateRefundPaymentCommand)
                .Publish(CreateCancelBookingCommand)
                .Publish(CreateReleaseReservationCommand)
                .TransitionTo(Compensating),

            When(ConfirmSeatsFaulted)
                .Then(context => context.Saga.FailureReason = FirstFaultMessage(context.Message))
                .Then(LogSagaState)
                .Publish(CreateRefundPaymentCommand)
                .Publish(CreateCancelBookingCommand)
                .Publish(CreateReleaseReservationCommand)
                .TransitionTo(Compensating));

        During(CompletingBooking,
            When(BookingMarkedPaid)
                .Then(context =>
                {
                    context.Saga.BookingPaid = true;
                    MarkCompleted(context.Saga);
                })
                .Then(LogSagaState)
                .Finalize(),

            When(MarkBookingPaidFaulted)
                .Then(context =>
                {
                    context.Saga.FailureReason = FirstFaultMessage(context.Message);
                    context.Saga.UpdatedAt = DateTime.UtcNow;
                })
                .Then(LogSagaState)
                .TransitionTo(CompletionFailed));

        During(Cancelling,
            When(BookingCancelled)
                .Then(context => context.Saga.BookingCancelled = true)
                .Then(LogSagaState)
                .If(
                    context => CancellationCompleted(context.Saga),
                    completed => completed
                        .Then(context => MarkCompleted(context.Saga))
                        .Finalize()),

            When(ReservationReleased)
                .Then(context => context.Saga.ReservationReleased = true)
                .Then(LogSagaState)
                .If(
                    context => CancellationCompleted(context.Saga),
                    completed => completed
                        .Then(context => MarkCompleted(context.Saga))
                        .Finalize()),

            When(CancelBookingFaulted)
                .Then(context => SetFailure(context.Saga, FirstFaultMessage(context.Message)))
                .TransitionTo(CancellationFailed),

            When(ReleaseReservationFaulted)
                .Then(context => SetFailure(context.Saga, FirstFaultMessage(context.Message)))
                .TransitionTo(CancellationFailed));

        During(Compensating,
            When(PaymentRefunded)
                .Then(context => context.Saga.PaymentRefunded = true)
                .Then(LogSagaState)
                .If(
                    context => CompensationCompleted(context.Saga),
                    completed => completed
                        .Then(context => MarkCompleted(context.Saga))
                        .Finalize()),

            When(BookingCancelled)
                .Then(context => context.Saga.BookingCancelled = true)
                .Then(LogSagaState)
                .If(
                    context => CompensationCompleted(context.Saga),
                    completed => completed
                        .Then(context => MarkCompleted(context.Saga))
                        .Finalize()),

            When(ReservationReleased)
                .Then(context => context.Saga.ReservationReleased = true)
                .Then(LogSagaState)
                .If(
                    context => CompensationCompleted(context.Saga),
                    completed => completed
                        .Then(context => MarkCompleted(context.Saga))
                        .Finalize()),

            When(RefundPaymentFaulted)
                .Then(context => SetFailure(context.Saga, FirstFaultMessage(context.Message)))
                .TransitionTo(CompensationFailed),

            When(CancelBookingFaulted)
                .Then(context => SetFailure(context.Saga, FirstFaultMessage(context.Message)))
                .TransitionTo(CompensationFailed),

            When(ReleaseReservationFaulted)
                .Then(context => SetFailure(context.Saga, FirstFaultMessage(context.Message)))
                .TransitionTo(CompensationFailed));

        SetCompletedWhenFinalized();
    }

    private void ConfigureCorrelations()
    {
        Event(() => BookingSubmitted, x => x.CorrelateById(context => context.Message.ReservationId));
        Event(() => PaymentSucceeded, x => x.CorrelateById(context => context.Message.ReservationId));
        Event(() => PaymentFailed, x => x.CorrelateById(context => context.Message.ReservationId));
        Event(() => PaymentTimedOut, x => x.CorrelateById(context => context.Message.ReservationId));
        Event(() => SeatsConfirmed, x => x.CorrelateById(context => context.Message.ReservationId));
        Event(() => SeatConfirmationFailed, x => x.CorrelateById(context => context.Message.ReservationId));
        Event(() => SeatConfirmationTimedOut, x => x.CorrelateById(context => context.Message.ReservationId));
        Event(() => BookingMarkedPaid, x => x.CorrelateById(context => context.Message.ReservationId));
        Event(() => BookingCancelled, x => x.CorrelateById(context => context.Message.ReservationId));
        Event(() => ReservationReleased, x => x.CorrelateById(context => context.Message.ReservationId));
        Event(() => PaymentRefunded, x => x.CorrelateById(context => context.Message.ReservationId));

        Event(() => RequestPaymentFaulted, x => x.CorrelateById(context => context.Message.Message.ReservationId));
        Event(() => ConfirmSeatsFaulted, x => x.CorrelateById(context => context.Message.Message.ReservationId));
        Event(() => MarkBookingPaidFaulted, x => x.CorrelateById(context => context.Message.Message.ReservationId));
        Event(() => CancelBookingFaulted, x => x.CorrelateById(context => context.Message.Message.ReservationId));
        Event(() => ReleaseReservationFaulted, x => x.CorrelateById(context => context.Message.Message.ReservationId));
        Event(() => RefundPaymentFaulted, x => x.CorrelateById(context => context.Message.Message.ReservationId));
    }

    private void InitializeSaga(BehaviorContext<BookingSaga, BookingSubmittedIntegrationEvent> context)
    {
        context.Saga.CorrelationId = context.Message.ReservationId;
        context.Saga.ReservationId = context.Message.ReservationId;
        context.Saga.BookingId = context.Message.BookingId;
        context.Saga.ShowtimeId = context.Message.ShowtimeId;
        context.Saga.UserId = context.Message.UserId;
        context.Saga.Seats = context.Message.SeatIds.ToArray();
        context.Saga.TotalPrice = context.Message.TotalPrice;
        context.Saga.ReservationVersion = context.Message.ReservationVersion;
        context.Saga.PreparedUntil = context.Message.PreparedUntil;
        context.Saga.CreatedAt = DateTime.UtcNow;
        context.Saga.UpdatedAt = DateTime.UtcNow;
        context.Saga.RequestId = context.RequestId;
        context.Saga.ResponseAddress = context.ResponseAddress;
    }

    private void LogSagaState<TEvent>(BehaviorContext<BookingSaga, TEvent> context)
        where TEvent : class
    {
        context.Saga.UpdatedAt = DateTime.UtcNow;
        _logger.LogInformation(
            "Booking saga {CorrelationId} handled {EventName} in state {State}",
            context.Saga.CorrelationId,
            context.Event.Name,
            context.Saga.CurrentState);
    }

    private static CancelBookingCommand CreateCancelBookingCommand<T>(
        BehaviorContext<BookingSaga, T> context)
        where T : class =>
        new(
            context.Saga.ReservationId,
            context.Saga.BookingId,
            context.Saga.FailureReason);

    private static ReleaseSeatReservationCommand CreateReleaseReservationCommand<T>(
        BehaviorContext<BookingSaga, T> context)
        where T : class =>
        new(
            context.Saga.ReservationId,
            context.Saga.BookingId,
            context.Saga.ShowtimeId,
            context.Saga.UserId);

    private static RefundPaymentCommand CreateRefundPaymentCommand<T>(
        BehaviorContext<BookingSaga, T> context)
        where T : class =>
        new(
            context.Saga.ReservationId,
            context.Saga.BookingId,
            context.Saga.PaymentId,
            context.Saga.TotalPrice,
            context.Saga.FailureReason);

    private static bool CancellationCompleted(BookingSaga saga) =>
        saga.BookingCancelled && saga.ReservationReleased;

    private static bool CompensationCompleted(BookingSaga saga) =>
        saga.PaymentRefunded && saga.BookingCancelled && saga.ReservationReleased;

    private static void MarkCompleted(BookingSaga saga)
    {
        saga.CompletedAt = DateTime.UtcNow;
        saga.UpdatedAt = DateTime.UtcNow;
    }

    private static void SetFailure(BookingSaga saga, string reason)
    {
        saga.FailureReason = reason;
        saga.UpdatedAt = DateTime.UtcNow;
    }

    private static string FirstFaultMessage<T>(Fault<T> fault)
        where T : class =>
        fault.Exceptions.FirstOrDefault()?.Message ?? $"{typeof(T).Name} failed";
}
