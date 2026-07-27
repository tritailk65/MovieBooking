using System.Text.Json.Serialization;

namespace Seat.API.Domain.Entities;

public record Seat
{
    public int ShowtimeId {get; set;}
    public string SeatId {get; set;}
    public SeatStatus SeatStatus {get; set;}
    public string LockedByUserId { get; set; } = string.Empty;
    public string LockToken {get; set;} = string.Empty;
    public DateTime LockExpiration { get; set; } = default!;
    // Price for seat
    public decimal BasePrice {get; set;}
}