using System.ComponentModel.DataAnnotations;

namespace BookingService.API.Infrastructure.Authentication;

public sealed class SeatServiceAuthenticationOptions
{
    public const string SectionName = "Grpc:SeatAuthentication";

    [Required, Url]
    public string TokenUrl { get; init; } = string.Empty;

    [Required]
    public string ClientId { get; init; } = string.Empty;

    [Required]
    public string ClientSecret { get; init; } = string.Empty;

    [Required]
    public string Scope { get; init; } = string.Empty;
}
