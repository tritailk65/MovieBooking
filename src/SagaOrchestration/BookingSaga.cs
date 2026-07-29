namespace SagaOrchestration;

using MassTransit;

public class BookingSaga : SagaStateMachineInstance
{
    // Payload Data
    public int BookingId {get; set;}
    public int ShowtimeId {get; set;}
    public string? UserId {get; set;}
    public string? ReservationId {get; set;}
    
    // State
    public Guid CorrelationId { get; set; }
    public string CurrentState { get; set; } = null!;
    public int Version { get; set; }
    
    // Audit info
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdateAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    
    // Callback 
    public Guid? RequestId { get; set; }
    public Uri? ResponseAddress { get; set; }
}

