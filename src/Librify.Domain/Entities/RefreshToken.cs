namespace Librify.Domain.Entities;

public class RefreshToken
{
    public Guid Id { get; set; }
    public required string Token { get; set; }
    public Guid UserId { get; set; }
    public Guid FamilyId { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public bool IsRevoked { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public bool IsValid => !IsRevoked && ExpiresAt > DateTimeOffset.UtcNow;
}
