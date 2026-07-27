namespace BookingService.API.Application.Validators;

public class IdentifiedCommandValidator : AbstractValidator<IdentifiedCommand<CreateBookingCommand, bool>>
{
    public IdentifiedCommandValidator(ILogger<IdentifiedCommandValidator> logger)
    {
        RuleFor(command => command.Id).NotEmpty();

        if (logger.IsEnabled(LogLevel.Trace))
        {
            logger.LogTrace("INSTANCE CREATED - {ClassName}", GetType().Name);
        }
    }
}
