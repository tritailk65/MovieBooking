// Old Catalog-local message contract. Kept as comments for reference only.
// Production publishing now uses
// SagaOrchestration.Contracts.ShowtimeCreatedIntegrationEvent.
//
// namespace Catalog.API.IntegrationEvents.Event;
//
// public record ShowtimeCreatedIntegrationEvent : IntegrationEvent
// {
//     public int ShowtimeId { get; init; }
//     public int HallId { get; init; }
//     public int MovieId { get; init; }
//     public DateTime StartTime { get; init; }
//     public DateTime EndTime { get; init; }
//     public decimal BasePrice { get; init; }
//     public IEnumerable<string> Seats { get; set; } = new List<string>();
// }
