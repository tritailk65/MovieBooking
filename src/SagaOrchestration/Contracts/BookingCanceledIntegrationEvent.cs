namespace SagaOrchestration;

public record BookingCanceledIntegrationEvent(int showtimeId, string userId) : IntegrationEvent;