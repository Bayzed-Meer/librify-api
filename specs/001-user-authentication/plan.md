# Implementation Plan: User Authentication

**Branch**: `001-user-authentication` | **Date**: 2026-05-06 | **Spec**: [spec.md](../../.specify/specs/001-user-authentication/spec.md)
**Input**: Feature specification from `.specify/specs/001-user-authentication/spec.md`

## Summary

Introduce the first identity layer for Librify: email/password registration, JWT-based login (HS256, 15-min access tokens), refresh token rotation with family invalidation on reuse detection, per-email brute-force rate limiting (10 attempts / 15 min), and logout. Implemented as Application-layer services over Domain entities persisted via EF Core in PostgreSQL.

## Technical Context

**Language/Version**: C# 13 / .NET 10  
**Primary Dependencies**: ASP.NET Core 10, EF Core 10 (Npgsql), `Microsoft.AspNetCore.Authentication.JwtBearer` 10.x, `Microsoft.Extensions.Identity.Core` (PasswordHasher), `IMemoryCache` (built-in) for rate limiting  
**Storage**: PostgreSQL via Npgsql EF Core  
**Testing**: xUnit + FluentAssertions + Moq (service unit tests only)  
**Target Platform**: Linux server / macOS development  
**Project Type**: REST API (web-service)  
**Performance Goals**: Stateless JWT verification — no DB lookup on protected endpoints (SC-004)  
**Constraints**: No hardcoded secrets; all token config via `appsettings.json` / User Secrets; no `.Result`/`.Wait()` anywhere  
**Scale/Scope**: Single user tier, personal library tool

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Clean Architecture Boundaries | ✅ PASS | Domain: entities + repo interfaces. Application: services + DTOs + service interfaces. Infrastructure: JWT service, password hasher, repositories, EF context. Api: controller + DI wiring only. |
| II. Async-First | ✅ PASS | All repository, service, and controller methods are async. `ConfigureAwait(false)` in Infrastructure/Application. |
| III. API Contract Standards | ✅ PASS | `[ApiController]`, `ActionResult<T>`, `[ProducesResponseType]` on all actions, `Problem()`/`ValidationProblem()` for all errors. |
| IV. Observability & Error Handling | ✅ PASS | `ILogger<T>` in all services. Specific exceptions: `DuplicateEmailException`, `InvalidCredentialsException`, `InvalidRefreshTokenException`. Global exception middleware already registered. |
| V. Test Coverage | ✅ PASS | Service unit tests required (non-optional per constitution). Controller and repository tests are out of scope. |
| VI. Simplicity & Explicitness | ✅ PASS | No premature abstractions. Explicit access modifiers. File-scoped namespaces. Records with `required` for all DTOs. |

No violations — Complexity Tracking table omitted.

## Project Structure

### Documentation (this feature)

```text
specs/001-user-authentication/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output
│   └── auth.md
└── tasks.md             # Phase 2 output (/speckit-tasks)
```

### Source Code

```text
src/
├── Librify.Domain/
│   ├── Entities/
│   │   ├── User.cs                              # NEW
│   │   └── RefreshToken.cs                      # NEW
│   └── Interfaces/
│       ├── IUserRepository.cs                   # NEW
│       └── IRefreshTokenRepository.cs           # NEW
│
├── Librify.Application/
│   └── Auth/
│       ├── IAuthService.cs                      # NEW
│       ├── AuthService.cs                       # NEW
│       ├── IJwtTokenService.cs                  # NEW
│       ├── ILoginAttemptTracker.cs              # NEW
│       ├── Exceptions/
│       │   ├── DuplicateEmailException.cs       # NEW
│       │   ├── InvalidCredentialsException.cs   # NEW
│       │   └── InvalidRefreshTokenException.cs  # NEW
│       └── Dtos/
│           ├── RegisterRequest.cs               # NEW
│           ├── LoginRequest.cs                  # NEW
│           ├── RefreshTokenRequest.cs           # NEW
│           ├── LogoutRequest.cs                 # NEW
│           └── AuthResponse.cs                  # NEW
│
├── Librify.Infrastructure/
│   ├── Auth/
│   │   ├── JwtTokenService.cs                   # NEW
│   │   └── LoginAttemptTracker.cs               # NEW
│   ├── Data/
│   │   └── AppDbContext.cs                      # MODIFY — add User + RefreshToken DbSets
│   ├── Migrations/                              # NEW — EF migration AddUserAuthentication
│   ├── Repositories/
│   │   ├── UserRepository.cs                    # NEW
│   │   └── RefreshTokenRepository.cs            # NEW
│   └── Extensions/
│       └── InfrastructureServiceExtensions.cs   # MODIFY — register auth services
│
└── Librify.Api/
    ├── Controllers/
    │   └── AuthController.cs                    # NEW
    └── Program.cs                               # MODIFY — add JWT authentication middleware

tests/
└── Librify.Tests/
    ├── Librify.Tests.csproj                     # NEW
    ├── Controllers/
    │   └── AuthControllerTests.cs               # NEW — WebApplicationFactory
    ├── Services/
    │   └── AuthServiceTests.cs                  # NEW — unit tests with Moq
    └── Repositories/
        ├── UserRepositoryTests.cs               # NEW — EF SQLite
        └── RefreshTokenRepositoryTests.cs       # NEW — EF SQLite
```

**Structure Decision**: Single `tests/Librify.Tests/` project per YAGNI (Principle VI). Test types separated by subdirectory mirroring source structure.
