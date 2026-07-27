using FluentValidation;

namespace BookingService.API.Application.Validators;

public class CreateBookingValidatorCommand : AbstractValidator<CreateBookingCommand>
{
    public CreateBookingValidatorCommand(ILogger<CreateBookingValidatorCommand> logger)
    {
        RuleFor(command => command.UserId).NotEmpty();
        RuleFor(command => command.UserName).NotEmpty();

        RuleFor(command => command.BookingItem).Must(ContainSeatItems).WithMessage("No order items found");

        RuleFor(command => command.ShowtimeId).NotEmpty();
        if (logger.IsEnabled(LogLevel.Trace))
        {
            logger.LogTrace("INSTANCE CREATED - {ClassName}", GetType().Name);
        }
    }

    private bool ContainSeatItems(IEnumerable<SeatItem> seatItems)
    {
        return seatItems.Any();
    }
}