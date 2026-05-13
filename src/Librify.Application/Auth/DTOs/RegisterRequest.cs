using System.ComponentModel.DataAnnotations;

namespace Librify.Application.Auth.Dtos;

public record RegisterRequest
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string DisplayName { get; init; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string Email { get; init; } = string.Empty;

    [Required]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,}$",
        ErrorMessage = "Password must be at least 8 characters and contain at least one uppercase letter, one lowercase letter, and one digit.")]
    public string Password { get; init; } = string.Empty;
}
