namespace Catalog.API.Application.Movies.Commands.UpdateMovie;

public class UpdateMovieCommandValitor : AbstractValidator<UpdateMovieCommand>
{
    public UpdateMovieCommandValitor(ILogger<UpdateMovieCommandValitor> logger)
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Movie Id is required.");
        RuleFor(x => x.Title).NotEmpty().WithMessage("Title is required.");
        RuleFor(x => x.Description).NotEmpty().WithMessage("Description is required.");
        RuleFor(x => x.DurationMinutes).GreaterThan(0).WithMessage("Duration must be greater than 0.");
        RuleFor(x => x.ReleaseDate).LessThanOrEqualTo(DateTime.UtcNow).WithMessage("Release date cannot be in the future.");
        RuleFor(x => x.Director).NotEmpty().WithMessage("Director is required.");
        RuleFor(x => x.Cast).NotEmpty().WithMessage("Cast is required.");
        RuleFor(x => x.TrailerUrl).NotEmpty().WithMessage("Trailer URL is required.")
            .Must(uri => Uri.IsWellFormedUriString(uri, UriKind.Absolute))
            .WithMessage("Trailer URL must be a valid URL.");
        RuleFor(x => x.PosterUrl).NotEmpty()
            .WithMessage("Poster URL is required.")
            .Must(uri => Uri.IsWellFormedUriString(uri, UriKind.Absolute))
            .WithMessage("Poster URL must be a valid URL.");
    }
}