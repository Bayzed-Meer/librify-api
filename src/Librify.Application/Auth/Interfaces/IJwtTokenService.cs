using Librify.Domain.Entities;

namespace Librify.Application.Auth.Interfaces;

public interface IJwtTokenService
{
    (string AccessToken, DateTimeOffset ExpiresAt) GenerateAccessToken(User user);
}
