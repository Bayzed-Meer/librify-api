# Research: User Authentication

## Decision 1: JWT Validation Package

**Decision**: Add `Microsoft.AspNetCore.Authentication.JwtBearer` (version `10.0.7`) to `Librify.Api`.

**Rationale**: This is the idiomatic ASP.NET Core package for validating HS256 JWT Bearer tokens via the `AddJwtBearer()` / `UseAuthentication()` middleware pipeline. Token validation (signature, expiry, issuer, audience) is handled automatically without manual parsing in controllers.

**Alternatives considered**: Manually parsing the `Authorization` header using `System.IdentityModel.Tokens.Jwt` — rejected because it duplicates what the framework middleware already provides and would require custom middleware.

---

## Decision 2: Password Hashing

**Decision**: Add `Microsoft.Extensions.Identity.Core` (version `10.0.7`) to `Librify.Infrastructure`. Register `IPasswordHasher<User>` / `PasswordHasher<User>` as Singleton via DI.

**Rationale**: Provides PBKDF2/HMACSHA512 (V3 format, 100,000 iterations) with no dependency on the full ASP.NET Core Identity stack (no UserManager, no Identity middleware, no extra schema). Confirmed in clarifications as the chosen algorithm.

**Alternatives considered**: `BCrypt.Net-Next` and `Isopoh.Cryptography.Argon2` — rejected in favour of the built-in option (no extra NuGet, NIST-approved, already in the .NET ecosystem).

---

## Decision 3: JWT Token Generation

**Decision**: Use `JwtSecurityTokenHandler` (transitively available via `JwtBearer` package) in `JwtTokenService` (Infrastructure). Claims: `sub` (userId as string), `email`, `jti` (new Guid per token), `iat`, `exp`.

**Rationale**: `JwtSecurityTokenHandler` is the stable, well-documented .NET API for creating signed JWTs. `JsonWebTokenHandler` (newer) is more performant but has different API; for a personal library tool `JwtSecurityTokenHandler` is sufficient and the documentation is richer.

**Alternatives considered**: `JsonWebTokenHandler` — valid but less documented; deferred to a future optimisation pass.

---

## Decision 4: Refresh Token Format

**Decision**: Store refresh tokens as raw `Guid` strings (`Guid.NewGuid().ToString("N")` — 32 hex chars, no hyphens).

**Rationale**: A v4 UUID is 128 bits of cryptographic randomness, making brute-force or collision attacks infeasible at this scale. No additional hashing of the token value is performed. Token is transmitted over HTTPS only and treated as a secret bearer credential.

**Alternatives considered**: SHA256-hashing the token before DB storage — valid security hardening but over-engineered for a single-user personal tool; deferred to a future security pass.

---

## Decision 5: Token Family for Reuse Detection (FR-013)

**Decision**: Add a `FamilyId: Guid` column to `RefreshToken`. On first login, a new `FamilyId` is generated. Each rotation inherits the same `FamilyId`. On reuse detection, all tokens sharing that `FamilyId` are revoked via a single bulk-update query.

**Rationale**: Family invalidation (OAuth 2.0 BCP §2.2.2) terminates the entire compromised session while leaving other independent sessions (different `FamilyId`) intact.

**Alternatives considered**: Revoking all tokens for the user regardless of family — overly broad; rejected.

---

## Decision 6: Per-Email Rate Limiting (FR-012)

**Decision**: Implement `LoginAttemptTracker` in `Librify.Infrastructure.Auth` backed by `IMemoryCache`. Key: normalised email. Threshold: 10 failed attempts. Window: 15 minutes sliding. Reset on successful login.

**Rationale**: `IMemoryCache` is built into ASP.NET Core (no extra package) and is sufficient for a single-instance deployment. Application-level tracking (rather than HTTP middleware) allows easy reset on success and per-email granularity.

**Alternatives considered**: `AddRateLimiter()` middleware — operates at HTTP level, doesn't support per-email keying or success-based reset; rejected. Redis distributed cache — over-engineered for single-instance personal tool.

---

## Decision 7: New NuGet Packages Required

| Project | Package | Version |
|---------|---------|---------|
| `Librify.Api` | `Microsoft.AspNetCore.Authentication.JwtBearer` | `10.0.7` |
| `Librify.Infrastructure` | `Microsoft.Extensions.Identity.Core` | `10.0.7` |
| `tests/Librify.Tests` | `Microsoft.NET.Test.Sdk` | latest |
| `tests/Librify.Tests` | `xunit` | latest |
| `tests/Librify.Tests` | `xunit.runner.visualstudio` | latest |
| `tests/Librify.Tests` | `FluentAssertions` | latest |
| `tests/Librify.Tests` | `Moq` | latest |
| `tests/Librify.Tests` | `Microsoft.AspNetCore.Mvc.Testing` | `10.0.7` |
| `tests/Librify.Tests` | `Microsoft.EntityFrameworkCore.Sqlite` | `10.0.7` |

---

## Decision 8: JWT Configuration Shape

```json
"JwtSettings": {
  "Issuer": "librify-api",
  "Audience": "librify-clients",
  "AccessTokenExpiryMinutes": 15,
  "RefreshTokenExpiryDays": 7
}
```

The `Secret` key is stored in .NET User Secrets during development (`dotnet user-secrets set "JwtSettings:Secret" "..."`) and sourced from environment variables in production. It must be at least 32 characters to satisfy HS256 key requirements.
