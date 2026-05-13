using FluentAssertions;
using Librify.Application.Auth.Dtos;
using Librify.Application.Auth.Exceptions;
using Librify.Application.Auth.Interfaces;
using Librify.Application.Auth.Repositories;
using Librify.Application.Auth.Services;
using Librify.Domain.Entities;
using MapsterMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;

namespace Librify.Tests.Services;

public class AuthServiceTests
{
    private const string TestPassword = "Passw0rd!";
    private const string WrongTestPassword = "WrongPassword1";

    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepo = new();
    private readonly Mock<IJwtTokenService> _jwtTokenService = new();
    private readonly Mock<ILoginAttemptTracker> _loginAttemptTracker = new();
    private readonly Mock<IPasswordHasher<User>> _passwordHasher = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly Mock<ILogger<AuthService>> _logger = new();

    private AuthService CreateSut() => new(
        _userRepo.Object,
        _refreshTokenRepo.Object,
        _jwtTokenService.Object,
        _loginAttemptTracker.Object,
        _passwordHasher.Object,
        _mapper.Object,
        _logger.Object);

    // ---- RegisterAsync ----

    [Fact]
    public async Task RegisterAsync_ValidRequest_CreatesUserWithLowercasedEmail()
    {
        _userRepo.Setup(x => x.FindByEmailAsync("jane@example.com")).ReturnsAsync((User?)null);
        _passwordHasher.Setup(x => x.HashPassword(It.IsAny<User>(), TestPassword)).Returns("hashed");
        _mapper.Setup(x => x.Map<RegisterResponse>(It.IsAny<User>()))
            .Returns((User u) => new RegisterResponse { UserId = u.Id, Email = u.Email });

        var sut = CreateSut();
        var result = await sut.RegisterAsync(new RegisterRequest
        {
            DisplayName = "Jane",
            Email = "Jane@Example.com",
            Password = TestPassword,
        });

        result.Email.Should().Be("jane@example.com");
        _userRepo.Verify(x => x.AddAsync(It.Is<User>(u => u.Email == "jane@example.com")), Times.Once());
        _userRepo.Verify(x => x.SaveChangesAsync(), Times.Once());
    }

    [Fact]
    public async Task RegisterAsync_DuplicateEmail_ThrowsDuplicateEmailException()
    {
        _userRepo.Setup(x => x.FindByEmailAsync("jane@example.com")).ReturnsAsync(new User
        {
            Id = Guid.NewGuid(),
            DisplayName = "Jane",
            Email = "jane@example.com",
            PasswordHash = "hash",
            CreatedAt = DateTimeOffset.UtcNow,
        });

        var sut = CreateSut();
        var act = () => sut.RegisterAsync(new RegisterRequest
        {
            DisplayName = "Jane",
            Email = "jane@example.com",
            Password = TestPassword,
        });

        await act.Should().ThrowAsync<DuplicateEmailException>()
            .WithMessage("*jane@example.com*");
    }

    [Fact]
    public async Task RegisterAsync_Success_PasswordNotStoredAsPlainText()
    {
        _userRepo.Setup(x => x.FindByEmailAsync("jane@example.com")).ReturnsAsync((User?)null);
        _passwordHasher.Setup(x => x.HashPassword(It.IsAny<User>(), TestPassword)).Returns("hashed_password");

        var sut = CreateSut();
        await sut.RegisterAsync(new RegisterRequest
        {
            DisplayName = "Jane",
            Email = "jane@example.com",
            Password = TestPassword,
        });

        _userRepo.Verify(x => x.AddAsync(It.Is<User>(u => u.PasswordHash != TestPassword)), Times.Once());
    }

    // ---- LoginAsync ----

    [Fact]
    public async Task LoginAsync_CorrectCredentials_ReturnsAuthResponse()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            DisplayName = "Jane",
            Email = "jane@example.com",
            PasswordHash = "hashed",
            CreatedAt = DateTimeOffset.UtcNow,
        };
        _loginAttemptTracker.Setup(x => x.IsBlocked("jane@example.com")).Returns(false);
        _userRepo.Setup(x => x.FindByEmailAsync("jane@example.com")).ReturnsAsync(user);
        _passwordHasher.Setup(x => x.VerifyHashedPassword(user, "hashed", TestPassword))
            .Returns(PasswordVerificationResult.Success);
        _jwtTokenService.Setup(x => x.GenerateAccessToken(user))
            .Returns(("access_token", DateTimeOffset.UtcNow.AddMinutes(15)));

        var sut = CreateSut();
        var result = await sut.LoginAsync(new LoginRequest
        {
            Email = "jane@example.com",
            Password = TestPassword,
        });

        result.AccessToken.Should().Be("access_token");
        result.RefreshToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_ThrowsInvalidCredentialsException()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            DisplayName = "Jane",
            Email = "jane@example.com",
            PasswordHash = "hashed",
            CreatedAt = DateTimeOffset.UtcNow,
        };
        _loginAttemptTracker.Setup(x => x.IsBlocked("jane@example.com")).Returns(false);
        _userRepo.Setup(x => x.FindByEmailAsync("jane@example.com")).ReturnsAsync(user);
        _passwordHasher.Setup(x => x.VerifyHashedPassword(user, "hashed", WrongTestPassword))
            .Returns(PasswordVerificationResult.Failed);

        var sut = CreateSut();
        var act = () => sut.LoginAsync(new LoginRequest
        {
            Email = "jane@example.com",
            Password = WrongTestPassword,
        });

        await act.Should().ThrowAsync<InvalidCredentialsException>();
    }

    [Fact]
    public async Task LoginAsync_UnknownEmail_ThrowsInvalidCredentialsException()
    {
        _loginAttemptTracker.Setup(x => x.IsBlocked("unknown@example.com")).Returns(false);
        _userRepo.Setup(x => x.FindByEmailAsync("unknown@example.com")).ReturnsAsync((User?)null);

        var sut = CreateSut();
        var act = () => sut.LoginAsync(new LoginRequest
        {
            Email = "unknown@example.com",
            Password = TestPassword,
        });

        await act.Should().ThrowAsync<InvalidCredentialsException>();
    }

    [Fact]
    public async Task LoginAsync_IsBlocked_ThrowsRateLimitExceededException()
    {
        _loginAttemptTracker.Setup(x => x.IsBlocked("jane@example.com")).Returns(true);

        var sut = CreateSut();
        var act = () => sut.LoginAsync(new LoginRequest
        {
            Email = "jane@example.com",
            Password = TestPassword,
        });

        await act.Should().ThrowAsync<RateLimitExceededException>();
    }

    [Fact]
    public async Task LoginAsync_SuccessfulLogin_ResetsFailureCounter()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            DisplayName = "Jane",
            Email = "jane@example.com",
            PasswordHash = "hashed",
            CreatedAt = DateTimeOffset.UtcNow,
        };
        _loginAttemptTracker.Setup(x => x.IsBlocked("jane@example.com")).Returns(false);
        _userRepo.Setup(x => x.FindByEmailAsync("jane@example.com")).ReturnsAsync(user);
        _passwordHasher.Setup(x => x.VerifyHashedPassword(user, "hashed", TestPassword))
            .Returns(PasswordVerificationResult.Success);
        _jwtTokenService.Setup(x => x.GenerateAccessToken(user))
            .Returns(("token", DateTimeOffset.UtcNow.AddMinutes(15)));

        var sut = CreateSut();
        await sut.LoginAsync(new LoginRequest
        {
            Email = "jane@example.com",
            Password = TestPassword,
        });

        _loginAttemptTracker.Verify(x => x.Reset("jane@example.com"), Times.Once());
    }

    // ---- RefreshTokenAsync ----

    [Fact]
    public async Task RefreshTokenAsync_ValidToken_ReturnsNewAuthResponse()
    {
        var familyId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var existingToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = "validtoken12345678901234567890",
            UserId = userId,
            FamilyId = familyId,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7),
            IsRevoked = false,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        var user = new User
        {
            Id = userId,
            DisplayName = "Jane",
            Email = "jane@example.com",
            PasswordHash = "hashed",
            CreatedAt = DateTimeOffset.UtcNow,
        };
        _refreshTokenRepo.Setup(x => x.FindByTokenAsync("validtoken12345678901234567890")).ReturnsAsync(existingToken);
        _userRepo.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync(user);
        _jwtTokenService.Setup(x => x.GenerateAccessToken(user))
            .Returns(("new_access_token", DateTimeOffset.UtcNow.AddMinutes(15)));

        var sut = CreateSut();
        var result = await sut.RefreshTokenAsync("validtoken12345678901234567890");

        result.AccessToken.Should().Be("new_access_token");
        result.RefreshToken.Should().NotBe("validtoken12345678901234567890");
    }

    [Fact]
    public async Task RefreshTokenAsync_OldTokenIsRevokedAfterRotation()
    {
        var existingToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = "validtoken12345678901234567890",
            UserId = Guid.NewGuid(),
            FamilyId = Guid.NewGuid(),
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7),
            IsRevoked = false,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        _refreshTokenRepo.Setup(x => x.FindByTokenAsync("validtoken12345678901234567890")).ReturnsAsync(existingToken);
        _userRepo.Setup(x => x.FindByIdAsync(existingToken.UserId)).ReturnsAsync(new User
        {
            Id = existingToken.UserId,
            DisplayName = "Jane",
            Email = "jane@example.com",
            PasswordHash = "hashed",
            CreatedAt = DateTimeOffset.UtcNow,
        });
        _jwtTokenService.Setup(x => x.GenerateAccessToken(It.IsAny<User>()))
            .Returns(("token", DateTimeOffset.UtcNow.AddMinutes(15)));

        var sut = CreateSut();
        await sut.RefreshTokenAsync("validtoken12345678901234567890");

        existingToken.IsRevoked.Should().BeTrue();
    }

    [Fact]
    public async Task RefreshTokenAsync_ExpiredToken_ThrowsInvalidRefreshTokenException()
    {
        var expiredToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = "expiredtoken123456789012345678",
            UserId = Guid.NewGuid(),
            FamilyId = Guid.NewGuid(),
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(-1),
            IsRevoked = false,
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-8),
        };
        _refreshTokenRepo.Setup(x => x.FindByTokenAsync("expiredtoken123456789012345678")).ReturnsAsync(expiredToken);

        var sut = CreateSut();
        var act = () => sut.RefreshTokenAsync("expiredtoken123456789012345678");

        await act.Should().ThrowAsync<InvalidRefreshTokenException>();
    }

    [Fact]
    public async Task RefreshTokenAsync_RevokedToken_ThrowsInvalidRefreshTokenException()
    {
        var revokedToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = "revokedtoken12345678901234567",
            UserId = Guid.NewGuid(),
            FamilyId = Guid.NewGuid(),
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7),
            IsRevoked = true,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        _refreshTokenRepo.Setup(x => x.FindByTokenAsync("revokedtoken12345678901234567")).ReturnsAsync(revokedToken);

        var sut = CreateSut();
        var act = () => sut.RefreshTokenAsync("revokedtoken12345678901234567");

        await act.Should().ThrowAsync<InvalidRefreshTokenException>();
    }

    [Fact]
    public async Task RefreshTokenAsync_ConsumedToken_RevokesFamilyAndThrows()
    {
        var familyId = Guid.NewGuid();
        var consumedToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = "consumedtoken1234567890123456",
            UserId = Guid.NewGuid(),
            FamilyId = familyId,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7),
            IsRevoked = true,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        _refreshTokenRepo.Setup(x => x.FindByTokenAsync("consumedtoken1234567890123456")).ReturnsAsync(consumedToken);

        var sut = CreateSut();
        var act = () => sut.RefreshTokenAsync("consumedtoken1234567890123456");

        await act.Should().ThrowAsync<InvalidRefreshTokenException>();
        _refreshTokenRepo.Verify(x => x.RevokeByFamilyIdAsync(familyId), Times.Once());
    }

    // ---- LogoutAsync ----

    [Fact]
    public async Task LogoutAsync_ValidToken_RevokesRefreshToken()
    {
        var token = new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = "validlogouttoken1234567890abc",
            UserId = Guid.NewGuid(),
            FamilyId = Guid.NewGuid(),
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7),
            IsRevoked = false,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        _refreshTokenRepo.Setup(x => x.FindByTokenAsync("validlogouttoken1234567890abc")).ReturnsAsync(token);

        var sut = CreateSut();
        await sut.LogoutAsync("validlogouttoken1234567890abc");

        token.IsRevoked.Should().BeTrue();
        _refreshTokenRepo.Verify(x => x.SaveChangesAsync(), Times.Once());
    }

    [Fact]
    public async Task LogoutAsync_TokenNotFound_ThrowsInvalidRefreshTokenException()
    {
        _refreshTokenRepo.Setup(x => x.FindByTokenAsync("unknowntoken12345678901234567")).ReturnsAsync((RefreshToken?)null);

        var sut = CreateSut();
        var act = () => sut.LogoutAsync("unknowntoken12345678901234567");

        await act.Should().ThrowAsync<InvalidRefreshTokenException>();
    }

    [Fact]
    public async Task LogoutAsync_AlreadyRevokedToken_ThrowsInvalidRefreshTokenException()
    {
        var revokedToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = "alreadyrevokedtoken12345678901",
            UserId = Guid.NewGuid(),
            FamilyId = Guid.NewGuid(),
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7),
            IsRevoked = true,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        _refreshTokenRepo.Setup(x => x.FindByTokenAsync("alreadyrevokedtoken12345678901")).ReturnsAsync(revokedToken);

        var sut = CreateSut();
        var act = () => sut.LogoutAsync("alreadyrevokedtoken12345678901");

        await act.Should().ThrowAsync<InvalidRefreshTokenException>();
    }
}
