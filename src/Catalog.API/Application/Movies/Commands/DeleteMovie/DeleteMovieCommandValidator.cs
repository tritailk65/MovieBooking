namespace Catalog.API.Application.Movies.Commands.DeleteMovie;

public class DeleteMovieCommandValidator : AbstractValidator<DeleteMovieCommand>
{
    public DeleteMovieCommandValidator(ILogger<DeleteMovieCommandValidator> logger)
    {
        RuleFor(x => x.Id).GreaterThan(0).WithMessage("Movie Id must be greater than 0.");
        
    }
}