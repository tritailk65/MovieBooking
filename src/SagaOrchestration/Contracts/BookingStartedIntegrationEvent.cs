namespace SagaOrchestration;

public record BookingStartedIntegrationEvent(string userId) : IntegrationEvent;