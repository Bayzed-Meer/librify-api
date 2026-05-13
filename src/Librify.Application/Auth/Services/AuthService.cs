using Librify.Application.Auth.Dtos;
using Librify.Application.Auth.Exceptions;
using Librify.Application.Auth.Interfaces;
using Librify.Application.Auth.Repositories;

using Librify.Domain.Entities;
using MapsterMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Librify.Application.Auth.Services;

public class AuthService(
    IUserRepository userRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IJwtTokenService jwtTokenService,
    ILoginAttemptTracker loginAttemptTracker,
    IPasswordHasher<User> passwordHasher,
    IMapper mapper,
    ILogger<AuthService> logger) : IAuthService
{
    public async Task<RegisterResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var normalisedEmail = request.Email.ToLowerInvariant();

        var existing = await userRepository.FindByEmailAsync(normalisedEmail, cancellationToken);
        
        if (existing is not null)
            throw new DuplicateEmailException(normalisedEmail);

        var user = new User
        {
            Id = Guid.NewGuid(),
            DisplayName = request.DisplayName,
            Email = normalisedEmail,
            PasswordHash = string.Empty,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);

        await userRepository.AddAsync(user, cancellationToken);
        await userRepository.SaveChangesAsync(cancellationToken);

        logger.LogInformation("User registered: {UserId}", user.Id);

        return mapper.Map<RegisterResponse>(user);
    }

    public async Task<AuthResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var normalisedEmail = request.Email.ToLowerInvariant();

        if (loginAttemptTracker.IsBlocked(normalisedEmail))
            throw new RateLimitExceededException();

        var user = await userRepository.FindByEmailAsync(normalisedEmail, cancellationToken);
        if (user is null)
        {
            loginAttemptTracker.RecordFailure(normalisedEmail);
            throw new InvalidCredentialsException();
        }

        var result = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (result == PasswordVerificationResult.Failed)
        {
            loginAttemptTracker.RecordFailure(normalisedEmail);
            throw new InvalidCredentialsException();
        }

        loginAttemptTracker.Reset(normalisedEmail);

        var tokenResult = jwtTokenService.GenerateAccessToken(user);

        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = Guid.NewGuid().ToString("N"),
            UserId = user.Id,
            FamilyId = Guid.NewGuid(),
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7),
            IsRevoked = false,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        await refreshTokenRepository.AddAsync(refreshToken, cancellationToken);
        await refreshTokenRepository.SaveChangesAsync(cancellationToken);

        logger.LogInformation("User logged in: {UserId}", user.Id);

        return new AuthResult
        {
            AccessToken = tokenResult.AccessToken,
            AccessTokenExpiresAt = tokenResult.ExpiresAt,
            RefreshToken = refreshToken.Token,
            RefreshTokenExpiresAt = refreshToken.ExpiresAt,
        };
    }

    public async Task<AuthResult> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var token = await refreshTokenRepository.FindByTokenAsync(refreshToken, cancellationToken);
        if (token is null)
            throw new InvalidRefreshTokenException();

        if (token.IsRevoked)
        {
            await refreshTokenRepository.RevokeByFamilyIdAsync(token.FamilyId, cancellationToken);
            throw new InvalidRefreshTokenException();
        }

        if (token.ExpiresAt <= DateTimeOffset.UtcNow)
            throw new InvalidRefreshTokenException();

        token.IsRevoked = true;
        await refreshTokenRepository.SaveChangesAsync(cancellationToken);

        var user = await userRepository.FindByIdAsync(token.UserId, cancellationToken)
            ?? throw new InvalidRefreshTokenException();

        var tokenResult = jwtTokenService.GenerateAccessToken(user);

        var newRefreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = Guid.NewGuid().ToString("N"),
            UserId = user.Id,
            FamilyId = token.FamilyId,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7),
            IsRevoked = false,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        await refreshTokenRepository.AddAsync(newRefreshToken, cancellationToken);
        await refreshTokenRepository.SaveChangesAsync(cancellationToken);

        return new AuthResult
        {
            AccessToken = tokenResult.AccessToken,
            AccessTokenExpiresAt = tokenResult.ExpiresAt,
            RefreshToken = newRefreshToken.Token,
            RefreshTokenExpiresAt = newRefreshToken.ExpiresAt,
        };
    }

    public async Task LogoutAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var token = await refreshTokenRepository.FindByTokenAsync(refreshToken, cancellationToken);
        if (token is null || token.IsRevoked)
            throw new InvalidRefreshTokenException();

        token.IsRevoked = true;
        await refreshTokenRepository.SaveChangesAsync(cancellationToken);
    }
}
