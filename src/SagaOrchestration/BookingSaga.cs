namespace SagaOrchestration;

public class BookingSaga : SagaStateMachineInstance
{
    public Guid CorrelationId { get; set; }
    public string CurrentState { get; set; } = null!;

    public Guid ReservationId { get; set; }
    public int BookingId { get; set; }
    public int ShowtimeId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string[] Seats { get; set; } = [];
    public decimal TotalPrice { get; set; }
    public int ReservationVersion { get; set; }
    public DateTime PreparedUntil { get; set; }
    public string? PaymentId { get; set; }

    public bool PaymentCaptured { get; set; }
    public bool SeatsConfirmed { get; set; }
    public bool BookingPaid { get; set; }
    public bool PaymentRefunded { get; set; }
    public bool BookingCancelled { get; set; }
    public bool ReservationReleased { get; set; }
    public string? FailureReason { get; set; }

    public int Version { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public Guid? RequestId { get; set; }
    public Uri? ResponseAddress { get; set; }
}
