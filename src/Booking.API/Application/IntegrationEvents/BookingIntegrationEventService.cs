
namespace BookingService.API.IntegrationEvents;


public class BookingIntegrationEventService (
    IEventBus eventBus,
    BookingContext bookingContext,
    IIntegrationEventLogService integrationEventLogService,
    ILogger<BookingIntegrationEventService> logger) : IBookingIntegrationEventService
{
    private readonly IEventBus _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
    private readonly BookingContext _bookingContext = bookingContext ?? throw new ArgumentNullException(nameof(bookingContext));
    private readonly IIntegrationEventLogService _eventLogService = integrationEventLogService ?? throw new ArgumentNullException(nameof(integrationEventLogService));
    private readonly ILogger<BookingIntegrationEventService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    // Lấy các event theo transaction Id rồi publish lên event bus
    public async Task PublishEventsThroughEventBusAsync(Guid transactionId)
    {
        var pendingLogEvents = await _eventLogService.RetrieveEventLogsPendingToPublishAsync(transactionId);

        foreach (var logEvt in pendingLogEvents)
        {
            _logger.LogInformation("Publishing integration event: {IntegrationEventId} - ({@IntegrationEvent})", logEvt.EventId, logEvt.IntegrationEvent);

            try
            {
                await _eventLogService.MarkEventAsInProgressAsync(logEvt.EventId);
                await _eventBus.PublishAsync(logEvt.IntegrationEvent);
                await _eventLogService.MarkEventAsPublishedAsync(logEvt.EventId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing integration event: {IntegrationEventId}", logEvt.EventId);

                await _eventLogService.MarkEventAsFailedAsync(logEvt.EventId);
            }
        }
    }

    // Luu event vao trong table event
    // Bahavior Transaction goi public event
    public async Task AddAndSaveEventAsync(IntegrationEvent evt)
    {
        _logger.LogInformation("Enqueuing integration event {IntegrationEventId} to repository ({@IntegrationEvent})", evt.Id, evt);

        await _eventLogService.SaveEventAsync(evt, _bookingContext.GetCurrentTransaction());
    }
}
