using BookingService.Domain.Exceptions;
using EventBus.Extensions;

namespace BookingService.API.Application.Behaviors;

public class ValidatorBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    private readonly ILogger<ValidatorBehavior<TRequest, TResponse>> _logger;
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidatorBehavior(IEnumerable<IValidator<TRequest>> validators, ILogger<ValidatorBehavior<TRequest, TResponse>> logger)
    {
        _validators = validators;
        _logger = logger;
    }

    public async Task<TResponse> Handle (TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var typeName = request.GetGenericTypeName();

        _logger.LogInformation("Validating command {command}", typeName);

        var validationTasks = _validators.Select(v => v.ValidateAsync(request,cancellationToken));
        var validationResult = await Task.WhenAll(validationTasks);

        var failure = validationResult.SelectMany(result => result.Errors).Where(error => error != null).ToList();

        if (failure.Any())
        {
            _logger.LogWarning("Validation errors - {CommandType} - Command: {@Command} - Errors: {@ValidationErrors}", typeName, request, failure);

            throw new BookingDomainException($"Command validation error for type {typeof(TRequest).Name}", new ValidationException("Validation Exception", failure));
        }

        return await next();
    }
}