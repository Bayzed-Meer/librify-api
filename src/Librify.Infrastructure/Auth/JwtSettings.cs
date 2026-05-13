using System.ComponentModel.DataAnnotations;

namespace Librify.Infrastructure.Auth;

public class JwtSettings
{
    [Required]
    public required string Secret { get; init; }

    [Required]
    public required string Issuer { get; init; }

    [Required]
    public required string Audience { get; init; }

    [Range(1, 1440)]
    public int AccessTokenExpiryMinutes { get; init; } = 15;

    [Range(1, 365)]
    public int RefreshTokenExpiryDays { get; init; } = 7;
}
