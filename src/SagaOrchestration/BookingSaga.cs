namespace SagaOrchestration;

using MassTransit;

public class BookingSaga : SagaStateMachineInstance
{
    // Payload Data
    public int BookingId {get; set;}
    public string? UserId {get; set;}
    public string? ReservationId {get; set;}
    public int ShowtimeId {get; init;}
    public int HallId {get; init;}
    public int MovieId {get; init;}
    public IEnumerable<string> Seats { get; set; } = new List<string>();
    
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

