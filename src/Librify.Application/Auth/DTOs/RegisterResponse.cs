namespace Librify.Application.Auth.Dtos;

public record RegisterResponse
{
    public required Guid UserId { get; init; }
    public required string Email { get; init; }
}
