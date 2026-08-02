namespace SagaOrchestration.Contracts;

public record ShowtimeCreatedIntegrationEvent(
     int ShowtimeId ,    
     int HallId ,
    int MovieId ,
     DateTime StartTime ,
     DateTime EndTime ,
     decimal BasePrice 
) : IntegrationEvent;