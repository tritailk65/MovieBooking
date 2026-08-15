using System.Transactions;
using BookingService.API.Application.IntegrationEvents;
using EventBus.Extensions;
using MediatR;

namespace BookingService.API.Application.Behaviors;

// MediatR pipeline behavior, mọi command/query đi qua handler thì có thể được bọc thêm logic trước và sau
public class TransactionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest  : IRequest<TResponse> // Đi qua handler này trước khi gọi vào command
{
    private readonly ILogger<TransactionBehavior<TRequest, TResponse>> _logger;
    private readonly BookingContext _dbContext;
    private readonly IBookingIntegrationEventService _bookingIntegrationEventService;

    public TransactionBehavior(
        BookingContext context, 
        ILogger<TransactionBehavior<TRequest, TResponse>> logger, 
        IBookingIntegrationEventService integrationEventService)
    {
        _logger = logger;
        _dbContext = context;
        _bookingIntegrationEventService = integrationEventService;
    }

    
    public async Task<TResponse> Handle (TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var response = default(TResponse);
        var typeName = request.GetGenericTypeName();

        try
        {
            if (_dbContext.HasActiveTransaction)
            {
                // Gọi tới handler chính luôn vì có transaction rồi
                return await next();
            }

            var strategy = _dbContext.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                Guid transactionId;

                await using var transaction = await _dbContext.BeginTransactionAsync();
                // Log để lưu transaction context
                using (_logger.BeginScope(new List<KeyValuePair<string, object>> { new("TransactionContext", transaction.TransactionId) }))
                {
                    _logger.LogInformation("Begin transaction {TransactionId} for {CommandName} ({@Command})", transaction.TransactionId, typeName, request);

                    response = await next();    // Gọi handler gốc của command

                    _logger.LogInformation("Commit transaction {TransactionId} for {CommandName}", transaction.TransactionId, typeName);

                    await _dbContext.CommitTransactionAsync(transaction);

                    transactionId = transaction.TransactionId;
                }

                // Publish event bus
                await _bookingIntegrationEventService.PublishEventsThroughEventBusAsync(transactionId);
            });

            return response;
        } 
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error Handling transaction for {CommandName} ({@Command})", typeName, request);

            throw;
        }
    }
}