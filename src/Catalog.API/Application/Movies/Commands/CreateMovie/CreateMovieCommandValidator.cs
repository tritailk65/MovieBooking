
namespace  Catalog.API.Application.Movies.Commands.CreateMovie;

public class CreateMovieCommandValidator : AbstractValidator<CreateMovieCommand>
{
    public CreateMovieCommandValidator(ILogger<CreateMovieCommandValidator> logger)
    {
        RuleFor(x => x.Tiltle)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(200).WithMessage("Title cannot exceed 200 characters.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required.");

        RuleFor(x => x.DurationMinutes)
            .GreaterThan(0).WithMessage("Duration must be greater than 0.");

        RuleFor(x => x.ReleaseDate)
            .LessThanOrEqualTo(DateTime.Now).WithMessage("Release date cannot be in the future.");

        RuleFor(x => x.Director)
            .NotEmpty().WithMessage("Director is required.")
            .MaximumLength(100).WithMessage("Director name cannot exceed 100 characters.");

        RuleFor(x => x.Cast)
            .NotEmpty().WithMessage("Cast is required.");

        RuleFor(x => x.TrailerUrl)
            .NotEmpty().WithMessage("Trailer URL is required.")
            .Must(uri => Uri.IsWellFormedUriString(uri, UriKind.Absolute)).WithMessage("Trailer URL must be a valid URL.");

        RuleFor(x => x.PosterUrl)
            .NotEmpty().WithMessage("Poster URL is required.")
            .Must(uri => Uri.IsWellFormedUriString(uri, UriKind.Absolute)).WithMessage("Poster URL must be a valid URL.");

        if (logger.IsEnabled(LogLevel.Trace))
        {
            logger.LogTrace("Instance created  - {claasname}", GetType().Name); 
        }
    }

    private bool BeAValidUrl(string url)
    {
        return Uri.IsWellFormedUriString(url, UriKind.Absolute);
    }

    private bool BeAValidReleaseDate(DateTime releaseDate)
    {
        return releaseDate <= DateTime.Now;
    }

}