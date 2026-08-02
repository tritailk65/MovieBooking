using SagaOrchestration.Contracts;

namespace SagaOrchestration;

public sealed class BookingStateMachine : MassTransitStateMachine<BookingSaga>
{
    private readonly ILogger<BookingStateMachine> _logger;

    public State ReservingSeat { get; private set; } = null!;
    public State Cancelling { get; private set; } = null!;
    public State PendingPayment { get; private set; } = null!;
    public State ExtendingSeatHold { get; private set; } = null!;
    public State PaymentProcessing { get; private set; } = null!;
    public State ReconcilingPayment { get; private set; } = null!;
    public State ConfirmingSeats { get; private set; } = null!;
    public State ResolvingUnknownPayment { get; private set; } = null!;
    public State Cancelled { get; private set; } = null!;
    public State RefundingLatePayment { get; private set; } = null!;
    public State Expiring { get; private set; } = null!;
    public State CompensationFailed { get; private set; } = null!;
    public State CancellationFailed { get; private set; } = null!;
    public State CompletingBooking { get; private set; } = null!;
    public State Compensating { get; private set; } = null!; 

    public Schedule<BookingSaga, BookingPendingPaymentExpiredIntegrationEvent> BookingScheduleExpired { get; private set; } = null!;
    
    public Event<BookingStatusChangedToSubmittedIntegrationEvent> BookingSubmitted { get; private set; } = null!;
    public Event<SeatReservationHeldIntegrationEvent> SeatReservationHeld { get; private set; } = null!;
    public Event<SeatHoldExtendedIntegrationEvent> SeatHoldExtended { get; private set; } = null!;
    public Event<SeatReservationFailedIntegrationEvent> SeatReservationFailed { get; private set; } = null!;
    public Event<SeatReservationTimedOutIntegrationEvent> SeatReservationTimedOut { get; private set; } = null!;
    public Event<PaymentRequestedIntegrationEvent> PaymentRequested { get; private set; } = null!;
    public Event<SeatReservationConfirmedIntegrationEvent> SeatsConfirmed { get; private set; } = null!;
    public Event<SeatReservationConfirmationFailedIntegrationEvent> SeatConfirmationFailed { get; private set; } = null!;
    public Event<SeatReservationConfirmationTimedOutIntegrationEvent> SeatConfirmationTimedOut { get; private set; } = null!;
    public Event<PaymentSucceededIntegrationEvent> PaymentSucceeded { get; private set; } = null!;
    public Event<PaymentFailedIntegrationEvent> PaymentFailed { get; private set; } = null!;
    public Event<PaymentTimedOutIntegrationEvent> PaymentTimedOut { get; private set; } = null!;
    public Event<BookingStatusChangedToPaidIntegrationEvent> BookingPaid  { get; private set; } = null!;
    public Event<BookingStatusChangedToCancelledIntegrationEvent> BookingCancelled { get; private set; } = null!;
    public Event<SeatReservationReleasedIntegrationEvent> ReservationReleased { get; private set; } = null!;
    public Event<BookingCancellationRequestedIntegrationEvent> BookingCancellationRequest { get; private set; } = null!;
    public Event<SeatHoldExtensionFailedIntegrationEvent> SeatHoldExtensionFailed { get; private set; } = null!;
    public Event<SeatHoldExtensionTimedOutIntegrationEvent> SeatHoldExtensionTimedOut { get; private set; } = null!;
    public Event<PaymentProviderConfirmedPaidIntegrationEvent> ProviderConfirmPaid { get; private set; } = null!;
    public Event<PaymentProviderConfirmedUnpaidIntegrationEvent> ProviderConfirmUnpaid  { get; private set; } = null!;
    public Event<PaymentRefundedIntegrationEvent> PaymentRefunded { get; private set; } = null!;
    public Event<LatePaymentSuccededIntegrationEvent> LatePaymentSucceded { get; private set; } = null!;
    public Event<BookingStatusChangedToExpiredIntegrationEvent> BookingExpired { get; private set; } = null!;
    public Event<SeatHoldDeadlineReachedIntegrationEvent> SeatHoldExpired  {get; private set; } = null!;

    public Event<Fault<ReserveSeatsCommand>> ReserveSeatFault { get; private set; } = null!;
    public Event<Fault<MarkBookingPaidCommand>> BookingStatusChangedPaidFaulted { get; private set; } = null!;
    public Event<Fault<ExtendSeatHoldCommand>> SeatHoldExtensionFault { get; private set; } = null!;
    public Event<Fault<ReleaseSeatReservationCommand>> ReleaseReservationFaulted { get; private set; } = null!;
    public Event<Fault<RefundPaymentCommand>> RefundPaymentFaulted { get; private set; } = null!;
    public Event<Fault<CancelBookingCommand>> CancelBookingFaulted { get; private set; } = null!;
    public Event<Fault<MarkBookingExpiredCommand>> MarkBookingExpiredFaulted { get; private set; } = null!;
    public Event<Fault<RequestPaymentCommand>> RequestPaymentFaulted { get; private set; } = null!;
    public Event<Fault<ConfirmSeatReservationCommand>> ConfirmSeatReservationFaulted { get; private set; } = null!;


    public BookingStateMachine(ILogger<BookingStateMachine> logger)
    {
        _logger = logger;

        InstanceState(x => x.CurrentState);

        Schedule(
            () => BookingScheduleExpired,
            saga => saga.PendingPaymentExpirationTokenId,
            configuration =>
            {
                configuration.Delay = TimeSpan.FromMinutes(10); // Timeout 10 phút đợi payment

                configuration.Received = e =>
                    e.CorrelateById(context =>
                        context.Message.ReservationId);
            });
 
        ConfigureCorrelations();

        Initially(
            When(BookingSubmitted)
                .Then(InitializeSaga)
                .Then(LogSagaState)
                .Publish(context => new ReserveSeatsCommand(
                    context.Saga.ReservationId,
                    context.Saga.BookingId,
                    context.Saga.ShowtimeId,
                    context.Saga.UserId,
                    context.Saga.ReservationVersion))
                .TransitionTo(ReservingSeat));

        During(ReservingSeat,
            When(SeatReservationHeld)   // Giữ ghế thành công
                .Then(LogSagaState)
                .Schedule(BookingScheduleExpired,
                    context => new BookingPendingPaymentExpiredIntegrationEvent(
                        context.Saga.ReservationId,
                        context.Saga.BookingId))
                .TransitionTo(PendingPayment),

            When(SeatReservationFailed) // giữ ghế fail
                .Then(context => context.Saga.FailureReason = context.Message.Reason)
                .Then(LogSagaState)
                .Publish(CreateCancelBookingCommand)
                .Publish(CreateReleaseReservationCommand)
                .TransitionTo(Cancelling),

            When(SeatReservationTimedOut)  // Quá thời gian kiểm tra giữ ghế
                .Then(context => context.Saga.FailureReason = "Seat reservation timed out")
                .Then(LogSagaState)
                .Publish(CreateCancelBookingCommand)
                .Publish(CreateReleaseReservationCommand)
                .TransitionTo(Cancelling),

            When(ReserveSeatFault)
                .Then(context => SetFailure(context.Saga, FirstFaultMessage(context.Message)))
                .Then(LogSagaState)
                .Publish(CreateCancelBookingCommand)
                .Publish(CreateReleaseReservationCommand)
                .TransitionTo(Cancelling)
        );

        During(PendingPayment,
            When(BookingScheduleExpired.Received)   // Khi nhan duoc mesage expire (chờ người dùng bấm thanh toán)
                .Then(context => context.Saga.FailureReason = "Pending payment expired after 10 minutes")
                .Publish(context => new MarkBookingExpiredCommand(
                    context.Saga.ReservationId,
                    context.Saga.BookingId))
                .Publish(CreateReleaseReservationCommand)
                .TransitionTo(Expiring),

            When(PaymentRequested)      // Booking xong việc, payment bắt đầu xử lý 
                .Unschedule(BookingScheduleExpired) 
                .Then(LogSagaState)
                .Publish(CreateExtendSeatHoldCommand)
                .TransitionTo(ExtendingSeatHold),    // Thêm thời gian giữ ghế, nên lớn hơn thời gian payment provider timeout
            
            When(BookingCancellationRequest)    // User cancel booking
                .Unschedule(BookingScheduleExpired) 
                .Then(LogSagaState)
                .Publish(CreateCancelBookingCommand)
                .Publish(CreateReleaseReservationCommand)
                .TransitionTo(Cancelling)        
        );

        During(ExtendingSeatHold,
            When(SeatHoldExtended)  // Gia hạn thời gian thành công
                .Then(LogSagaState)
                .Publish(context => new RequestPaymentCommand(  // Request service payment
                    context.Saga.ReservationId,
                    context.Saga.BookingId,
                    context.Saga.UserId,
                    context.Saga.TotalPrice))
                .TransitionTo(PaymentProcessing),    

            When(SeatHoldExtensionFailed)
                .Then(LogSagaState)
                .Publish(CreateCancelBookingCommand)
                .Publish(CreateReleaseReservationCommand)
                .TransitionTo(Cancelling),

            When(SeatHoldExtensionTimedOut)  
                .Then(context => context.Saga.FailureReason = "Seat hold extension timed out")
                .Then(LogSagaState)
                .Publish(CreateCancelBookingCommand)
                .Publish(CreateReleaseReservationCommand)
                .TransitionTo(Cancelling),

            When(SeatHoldExtensionFault)
                .Then(context => SetFailure(context.Saga, FirstFaultMessage(context.Message)))
                .Publish(CreateReleaseReservationCommand)
                .Publish(CreateCancelBookingCommand)
                .TransitionTo(Cancelling)
        );

        During(PaymentProcessing,
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

            When(PaymentFailed) //Thanh toán thất bại
                .Then(context => context.Saga.FailureReason = context.Message.Reason)
                .Then(LogSagaState)
                .Publish(CreateCancelBookingCommand)
                .Publish(CreateReleaseReservationCommand)
                .TransitionTo(Cancelling),

            When(PaymentTimedOut)   // Payment provider timeout
                .Then(context => context.Saga.FailureReason = "Payment timed out")
                .Then(LogSagaState)
                .TransitionTo(ReconcilingPayment),

            When(RequestPaymentFaulted)
                .Then(context => SetFailure(context.Saga, FirstFaultMessage(context.Message)))
                .Publish(CreateReleaseReservationCommand)
                .Publish(CreateCancelBookingCommand)
                .TransitionTo(Cancelling)
        );

        During(ReconcilingPayment,
            When(ProviderConfirmPaid)  
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

            When(SeatHoldExpired)
                .Then(LogSagaState)
                .TransitionTo(ResolvingUnknownPayment),
            
            When(ProviderConfirmUnpaid) // Third-party xác nhận chưa thanh toán
                .Then(LogSagaState)
                .Publish(CreateCancelBookingCommand)    // Cancel booking
                .Publish(CreateReleaseReservationCommand)   // Release Seat
                .TransitionTo(Cancelling)
        );

        During(ResolvingUnknownPayment,
            When(LatePaymentSucceded)
                .Then(LogSagaState)
                .TransitionTo(RefundingLatePayment),

            When(ProviderConfirmUnpaid) // Third-party xác nhận chưa thanh toán
                .Then(LogSagaState)
                .Publish(CreateCancelBookingCommand)    // Cancel booking
                .Publish(CreateReleaseReservationCommand)   // Release Seat
                .TransitionTo(Cancelling)
        );

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

            When(ConfirmSeatReservationFaulted)
                .Then(context => SetFailure(context.Saga, FirstFaultMessage(context.Message)))
                .Publish(CreateReleaseReservationCommand)
                .Publish(CreateCancelBookingCommand)
                .Publish(CreateRefundPaymentCommand)
                .TransitionTo(Compensating)
        );

        During(CompletingBooking,
            When(BookingPaid)
                .Then(context =>
                {
                    context.Saga.BookingPaid = true;
                    MarkCompleted(context.Saga);
                })
                .Then(LogSagaState)              
                .Finalize(),

            When(BookingStatusChangedPaidFaulted)
                .Then(context =>
                {
                    context.Saga.FailureReason = FirstFaultMessage(context.Message);
                    context.Saga.UpdatedAt = DateTime.UtcNow;
                })
                .Publish(CreateReleaseReservationCommand)
                .Publish(CreateCancelBookingCommand)
                .Publish(CreateRefundPaymentCommand)
                .TransitionTo(Compensating)
        );

        During(RefundingLatePayment,
            When(PaymentRefunded)   // Chỉ cần refund payment thôi
                .Then(context => { context.Saga.PaymentRefunded = true;})
                .Then(LogSagaState)
                .If(
                    // predicate
                    context => context.Saga.PaymentRefunded, // Chỉ chờ PaymentRefunded, đảm bảm các state compensation còn lại
                    completed => completed
                        .Then(context => MarkCompleted(context.Saga))
                        .Finalize())        
        );
    
        During(Expiring,
            When(BookingExpired)
                .Then(context => context.Saga.BookingExpired = true)
                .If(
                    context => ExpirationCompleted(context.Saga),
                    completed => completed
                        .Then(context => MarkCompleted(context.Saga))
                        .Finalize()),

            When(ReservationReleased)
                .Then(context => context.Saga.ReservationReleased = true)
                .If(
                    context => ExpirationCompleted(context.Saga),
                    completed => completed
                        .Then(context => MarkCompleted(context.Saga))
                        .Finalize()),

            When(MarkBookingExpiredFaulted)
                .Then(context => SetFailure(context.Saga, FirstFaultMessage(context.Message)))
                .Publish(CreateReleaseReservationCommand)
                .TransitionTo(Cancelling)
        );

        During(Compensating,
            When(PaymentRefunded)
                .Then(context => { context.Saga.PaymentRefunded = true;})
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

            When(BookingStatusChangedPaidFaulted)
                .Then(context => SetFailure(context.Saga, FirstFaultMessage(context.Message)))
                .TransitionTo(CompensationFailed),

            When(ReleaseReservationFaulted)
                .Then(context => SetFailure(context.Saga, FirstFaultMessage(context.Message)))
                .TransitionTo(CompensationFailed));

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

        SetCompletedWhenFinalized();
    }

    private void ConfigureCorrelations()
    {
        Event(() => BookingSubmitted, x => x.CorrelateById(context => context.Message.ReservationId));
        Event(() => SeatsConfirmed, x => x.CorrelateById(context => context.Message.ReservationId));
        Event(() => SeatReservationHeld, x => x.CorrelateById(context => context.Message.ReservationId));
        Event(() => SeatHoldExtended, x => x.CorrelateById(context => context.Message.ReservationId));
        Event(() => SeatReservationFailed, x => x.CorrelateById(context => context.Message.ReservationId));
        Event(() => SeatReservationTimedOut, x => x.CorrelateById(context => context.Message.ReservationId));
        Event(() => SeatConfirmationFailed, x => x.CorrelateById(context => context.Message.ReservationId));
        Event(() => SeatConfirmationTimedOut, x => x.CorrelateById(context => context.Message.ReservationId));
        Event(() => PaymentSucceeded, x => x.CorrelateById(context => context.Message.ReservationId));
        Event(() => PaymentFailed, x => x.CorrelateById(context => context.Message.ReservationId));
        Event(() => PaymentTimedOut, x => x.CorrelateById(context => context.Message.ReservationId));
        Event(() => BookingPaid, x => x.CorrelateById(context => context.Message.ReservationId));
        Event(() => BookingCancelled, x => x.CorrelateById(context => context.Message.ReservationId));
        Event(() => ReservationReleased, x => x.CorrelateById(context => context.Message.ReservationId));
        Event(() => BookingCancellationRequest, x => x.CorrelateById(context => context.Message.ReservationId));
        Event(() => ProviderConfirmPaid, x => x.CorrelateById(context => context.Message.ReservationId));
        Event(() => ProviderConfirmUnpaid, x => x.CorrelateById(context => context.Message.ReservationId));
        Event(() => PaymentRefunded, x => x.CorrelateById(context => context.Message.ReservationId));
        Event(() => LatePaymentSucceded, x => x.CorrelateById(context => context.Message.ReservationId));
        Event(() => BookingExpired, x => x.CorrelateById(context => context.Message.ReservationId));
        Event(() => SeatHoldExtensionFailed, x => x.CorrelateById(context => context.Message.ReservationId));
        Event(() => SeatHoldExtensionTimedOut, x => x.CorrelateById(context => context.Message.ReservationId));
        Event(() => SeatHoldExpired, x => x.CorrelateById(context => context.Message.ReservationId));
        Event(() => PaymentRequested, x => x.CorrelateById(context => context.Message.ReservationId));


        Event(() => ReserveSeatFault, x => x.CorrelateById(context => context.Message.Message.ReservationId));
        Event(() => BookingStatusChangedPaidFaulted, x => x.CorrelateById(context => context.Message.Message.ReservationId));
        Event(() => ReleaseReservationFaulted, x => x.CorrelateById(context => context.Message.Message.ReservationId));
        Event(() => RefundPaymentFaulted, x => x.CorrelateById(context => context.Message.Message.ReservationId));
        Event(() => CancelBookingFaulted, x => x.CorrelateById(context => context.Message.Message.ReservationId));
        Event(() => RequestPaymentFaulted, x => x.CorrelateById(context => context.Message.Message.ReservationId));
        Event(() => SeatHoldExtensionFault, x => x.CorrelateById(context => context.Message.Message.ReservationId));
        Event(() => ConfirmSeatReservationFaulted, x => x.CorrelateById(context => context.Message.Message.ReservationId));
        Event(() => MarkBookingExpiredFaulted, x => x.CorrelateById(context => context.Message.Message.ReservationId));

    }

    private void InitializeSaga(BehaviorContext<BookingSaga, BookingStatusChangedToSubmittedIntegrationEvent> context)
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

    private static ExtendSeatHoldCommand CreateExtendSeatHoldCommand<T>(
        BehaviorContext<BookingSaga, T> context)
        where T : class =>
        new(
            context.Saga.ReservationId,
            context.Saga.BookingId,
            context.Saga.ShowtimeId,
            context.Saga.UserId);
    

    private static bool CancellationCompleted(BookingSaga saga) =>
        saga.BookingCancelled && saga.ReservationReleased;

    private static bool CompensationCompleted(BookingSaga saga) =>
        saga.PaymentRefunded && saga.BookingCancelled && saga.ReservationReleased;

    private static bool ExpirationCompleted(BookingSaga saga) =>
        saga.BookingExpired &&
        saga.ReservationReleased;

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
