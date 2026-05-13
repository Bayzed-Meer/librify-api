namespace Librify.Application.Auth.Dtos;

public record AuthResponse
{
    public required string AccessToken { get; init; }
    public required DateTimeOffset ExpiresAt { get; init; }
}
