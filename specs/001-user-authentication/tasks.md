# Tasks: User Authentication

**Input**: Design documents from `specs/001-user-authentication/`
**Prerequisites**: plan.md ✅ spec.md ✅ research.md ✅ data-model.md ✅ contracts/auth.md ✅ quickstart.md ✅

**Tests**: Service unit tests required (Principle V). Controller and repository tests are out of scope per project convention.

**Organization**: Tasks are grouped by user story. Phases 1–2 are foundational prerequisites; Phases 3–6 each deliver one independently testable user story.

## Format: `[ID] [P?] [Story?] Description`

- **[P]**: Can run in parallel (different files, no shared dependencies within the phase)
- **[Story]**: Which user story this task belongs to (US1–US4)

---

## Phase 1: Setup

**Purpose**: Add packages and scaffold the test project so all subsequent phases can compile and run tests.

- [X] T001 Scaffold xunit test project and add to solution: `dotnet new xunit -o tests/Librify.Tests --framework net10.0 && dotnet sln add tests/Librify.Tests/Librify.Tests.csproj && dotnet add tests/Librify.Tests/Librify.Tests.csproj reference src/Librify.Api/Librify.Api.csproj src/Librify.Application/Librify.Application.csproj src/Librify.Infrastructure/Librify.Infrastructure.csproj`
- [X] T002 [P] Add `Microsoft.AspNetCore.Authentication.JwtBearer 10.0.7` to `src/Librify.Api/Librify.Api.csproj`
- [X] T003 [P] Add `Microsoft.Extensions.Identity.Core 10.0.7` to both `src/Librify.Infrastructure/Librify.Infrastructure.csproj` AND `src/Librify.Application/Librify.Application.csproj` — Application needs the `IPasswordHasher<T>` interface directly; adding a pure-interface package is not a Clean Architecture violation
- [X] T004 Add test NuGet packages to `tests/Librify.Tests/Librify.Tests.csproj`: `Microsoft.NET.Test.Sdk`, `xunit`, `xunit.runner.visualstudio`, `FluentAssertions`, `Moq`, `Microsoft.AspNetCore.Mvc.Testing 10.0.7`, `Microsoft.EntityFrameworkCore.Sqlite 10.0.7`
- [X] T005 Add `JwtSettings` section (Issuer, Audience, AccessTokenExpiryMinutes, RefreshTokenExpiryDays — no Secret value) to `src/Librify.Api/appsettings.json`; add `<UserSecretsId>` element to `src/Librify.Api/Librify.Api.csproj` (generate a new Guid), then run `dotnet user-secrets init --project src/Librify.Api`

- [X] T005b Add `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` inside `<PropertyGroup>` in `src/Librify.Api/Librify.Api.csproj`, `src/Librify.Application/Librify.Application.csproj`, `src/Librify.Infrastructure/Librify.Infrastructure.csproj`, `src/Librify.Domain/Librify.Domain.csproj`, and `tests/Librify.Tests/Librify.Tests.csproj` — constitution quality gate requires zero warnings

**Checkpoint**: Solution builds and `dotnet test` runs (zero tests, zero failures)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Domain entities, interfaces, DTOs, exceptions, infrastructure implementations, EF migration, and middleware wiring. No user story work can begin until this phase is complete.

**⚠️ CRITICAL**: All tasks in the parallel group (T006–T020b) touch different files and can run simultaneously. T021+ have explicit ordering.

### Domain layer (all parallel — different files)

- [X] T006 [P] Create `User` entity in `src/Librify.Domain/Entities/User.cs` with properties: `Guid Id`, `string DisplayName`, `string Email` (normalised to lowercase on write), `string PasswordHash`, `DateTimeOffset CreatedAt`
- [X] T007 [P] Create `RefreshToken` entity in `src/Librify.Domain/Entities/RefreshToken.cs` with properties: `Guid Id`, `string Token`, `Guid UserId`, `Guid FamilyId`, `DateTimeOffset ExpiresAt`, `bool IsRevoked`, `DateTimeOffset CreatedAt`; add computed `bool IsValid => !IsRevoked && ExpiresAt > DateTimeOffset.UtcNow`
- [X] T008 [P] Create `IUserRepository` interface in `src/Librify.Domain/Interfaces/IUserRepository.cs` with methods: `FindByEmailAsync`, `FindByIdAsync`, `AddAsync`, `SaveChangesAsync` (signatures from data-model.md)
- [X] T009 [P] Create `IRefreshTokenRepository` interface in `src/Librify.Domain/Interfaces/IRefreshTokenRepository.cs` with methods: `FindByTokenAsync`, `AddAsync`, `RevokeByFamilyIdAsync`, `SaveChangesAsync` (signatures from data-model.md)

### Application layer — interfaces (all parallel — different files)

- [X] T010 [P] Create `IJwtTokenService` interface in `src/Librify.Application/Auth/IJwtTokenService.cs` with method: `(string AccessToken, DateTimeOffset ExpiresAt) GenerateAccessToken(User user)`
- [X] T011 [P] Create `ILoginAttemptTracker` interface in `src/Librify.Application/Auth/ILoginAttemptTracker.cs` with methods: `bool IsBlocked(string normalisedEmail)`, `void RecordFailure(string normalisedEmail)`, `void Reset(string normalisedEmail)`
- [X] T012 [P] Create `IAuthService` interface in `src/Librify.Application/Auth/IAuthService.cs` with methods: `RegisterAsync`, `LoginAsync`, `RefreshTokenAsync`, `LogoutAsync` returning appropriate DTOs and `CancellationToken` parameters

### Application layer — DTOs (all parallel — different files)

- [X] T013 [P] Create `RegisterRequest` record in `src/Librify.Application/Auth/Dtos/RegisterRequest.cs` with `required` properties: `DisplayName` (`[Required]`, `[StringLength(100, MinimumLength = 1)]`), `Email` (`[Required]`, `[EmailAddress]`, `[StringLength(256)]`), `Password` (`[Required]`, `[RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,}$", ErrorMessage = "Password must be at least 8 characters and contain at least one uppercase letter, one lowercase letter, and one digit.")]`)
- [X] T014 [P] Create `LoginRequest` record in `src/Librify.Application/Auth/Dtos/LoginRequest.cs` with `required` properties: `Email` (`[Required]`, `[EmailAddress]`), `Password` (`[Required]`)
- [X] T015 [P] Create `RefreshTokenRequest` record in `src/Librify.Application/Auth/Dtos/RefreshTokenRequest.cs` with `required` property: `RefreshToken` (`[Required]`)
- [X] T016 [P] Create `LogoutRequest` record in `src/Librify.Application/Auth/Dtos/LogoutRequest.cs` with `required` property: `RefreshToken` (`[Required]`)
- [X] T017 [P] Create `AuthResponse` record in `src/Librify.Application/Auth/Dtos/AuthResponse.cs` with `required` properties: `string AccessToken`, `string RefreshToken`, `DateTimeOffset ExpiresAt`

### Application layer — exceptions (all parallel — different files)

- [X] T018 [P] Create `DuplicateEmailException` in `src/Librify.Application/Auth/Exceptions/DuplicateEmailException.cs` (extends `Exception`; accepts email in constructor; exposes `Email` property)
- [X] T019 [P] Create `InvalidCredentialsException` in `src/Librify.Application/Auth/Exceptions/InvalidCredentialsException.cs` (extends `Exception`; no extra fields)
- [X] T020 [P] Create `InvalidRefreshTokenException` in `src/Librify.Application/Auth/Exceptions/InvalidRefreshTokenException.cs` (extends `Exception`; no extra fields)
- [X] T020b [P] Create `RateLimitExceededException` in `src/Librify.Application/Auth/Exceptions/RateLimitExceededException.cs` (extends `Exception`; no extra fields) — distinct from `InvalidCredentialsException` so the controller and middleware can map it to 429 rather than 401

### Application layer — service skeleton (depends on T008–T020b)

- [X] T021 Create `AuthService` skeleton in `src/Librify.Application/Auth/AuthService.cs` implementing `IAuthService`; inject `IUserRepository`, `IRefreshTokenRepository`, `IJwtTokenService`, `ILoginAttemptTracker`, `IPasswordHasher<User>`, `ILogger<AuthService>`; stub all four methods with `throw new NotImplementedException()`

### Infrastructure layer (ordered by dependency)

- [X] T022 Modify `src/Librify.Infrastructure/Data/AppDbContext.cs` to add `DbSet<User> Users` and `DbSet<RefreshToken> RefreshTokens`; add `OnModelCreating` configuration from data-model.md (unique indexes, max lengths, FK cascade delete, FamilyId index) — depends on T006, T007
- [X] T023 [P] Implement `JwtTokenService` in `src/Librify.Infrastructure/Auth/JwtTokenService.cs` using `JwtSecurityTokenHandler`; inject `IConfiguration` and `ILogger<JwtTokenService>`; read `JwtSettings` from config; sign with HS256; include claims: `sub` (userId), `email`, `jti` (new Guid), `iat`, `exp`; return token string and `ExpiresAt`; use `ConfigureAwait(false)` on all awaited calls — depends on T010, T006
- [X] T024 [P] Implement `LoginAttemptTracker` in `src/Librify.Infrastructure/Auth/LoginAttemptTracker.cs` using `IMemoryCache`; inject `ILogger<LoginAttemptTracker>`; key: normalised email; threshold: 10 failures; window: sliding 15 minutes; `Reset` removes the cache entry — depends on T011
- [X] T025 Implement `UserRepository` in `src/Librify.Infrastructure/Repositories/UserRepository.cs` implementing `IUserRepository`; use `AsNoTracking()` for read operations; normalise email to lowercase in `FindByEmailAsync` — depends on T008, T022
- [X] T026 Implement `RefreshTokenRepository` in `src/Librify.Infrastructure/Repositories/RefreshTokenRepository.cs` implementing `IRefreshTokenRepository`; `RevokeByFamilyIdAsync` bulk-updates all tokens with matching `FamilyId` setting `IsRevoked = true` — depends on T009, T022
- [X] T027 Generate EF migration: `dotnet ef migrations add AddUserAuthentication --project src/Librify.Infrastructure --startup-project src/Librify.Api`; verify migration creates `Users` and `RefreshTokens` tables with all indexes — depends on T022
- [X] T028 Modify `src/Librify.Infrastructure/Extensions/InfrastructureServiceExtensions.cs` to register: `IAuthService → AuthService` (Scoped), `IJwtTokenService → JwtTokenService` (Scoped), `ILoginAttemptTracker → LoginAttemptTracker` (Singleton), `IUserRepository → UserRepository` (Scoped), `IRefreshTokenRepository → RefreshTokenRepository` (Scoped), `IPasswordHasher<User> → PasswordHasher<User>` (Singleton) — depends on T021, T023, T024, T025, T026

### API layer wiring (depends on T005, T018–T020)

- [X] T029 [P] Modify `src/Librify.Api/Program.cs` to add `services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(...)` binding `JwtSettings` from config (validate issuer, audience, lifetime, signing key); add `app.UseAuthentication()` and `app.UseAuthorization()` before `app.MapControllers()`
- [X] T030 Create `src/Librify.Api/Middleware/GlobalExceptionHandler.cs` implementing `IExceptionHandler` (ASP.NET Core 8+ built-in); map: `DuplicateEmailException → 409` (detail: `"Email already in use."`), `InvalidCredentialsException → 401` (detail: `"Invalid credentials."`), `InvalidRefreshTokenException → 401` (detail: `"Invalid or expired refresh token."`), `RateLimitExceededException → 429` (detail: `"Too many failed login attempts. Try again later."`); all errors use `TypedResults.Problem`; register via `builder.Services.AddExceptionHandler<GlobalExceptionHandler>()` in `src/Librify.Api/Program.cs` (before `Build()`); the existing `app.UseExceptionHandler()` already invokes it — depends on T018, T019, T020, T020b, T029

**Checkpoint**: `dotnet build` succeeds with zero errors; `dotnet test` runs (tests may fail — implementations are stubs)

---

## Phase 3: User Story 1 — Register a New Account (Priority: P1) 🎯 MVP

**Goal**: A new visitor can create an account via `POST /api/auth/register`; duplicate emails and invalid input are rejected.

**Independent Test**: Submit a valid registration request → 201 Created with `userId` and `email`; submit a duplicate email → 409; submit invalid data → 400.

### Tests for User Story 1

> Test method naming: `MethodName_StateUnderTest_ExpectedBehavior` (e.g. `RegisterAsync_DuplicateEmail_ThrowsDuplicateEmailException`)

- [X] T031 [US1] Write failing unit tests for `AuthService.RegisterAsync` in `tests/Librify.Tests/Services/AuthServiceTests.cs`: success path creates user with lowercased email and hashed password; duplicate email throws `DuplicateEmailException`; password is never stored as plain text

### Implementation for User Story 1

- [X] T034 [US1] Implement `AuthService.RegisterAsync` in `src/Librify.Application/Auth/AuthService.cs`: normalise email to lowercase (`ToLowerInvariant()`); check for duplicate via `IUserRepository.FindByEmailAsync` (throw `DuplicateEmailException` on match); hash password via `IPasswordHasher<User>.HashPassword`; construct `User` with `Guid.NewGuid()` and `DateTimeOffset.UtcNow`; call `AddAsync` + `SaveChangesAsync`; return registered user; use `ConfigureAwait(false)` on all awaited calls — depends on T021, T025
- [X] T035 [US1] Create `AuthController` in `src/Librify.Api/Controllers/AuthController.cs` with `[ApiController]`, `[Route("api/auth")]`; inject `IAuthService`; implement `POST /api/auth/register` action returning `ActionResult<object>` with `[ProducesResponseType(201)]`, `[ProducesResponseType(400)]`, `[ProducesResponseType(409)]`; call `AuthService.RegisterAsync`; return `Created` with `{ userId, email }` — depends on T034

**Checkpoint**: `POST /api/auth/register` works end-to-end; all User Story 1 tests pass

---

## Phase 4: User Story 2 — Log In to an Existing Account (Priority: P1)

**Goal**: A registered user can log in via `POST /api/auth/login` and receive an access token + refresh token; wrong credentials and rate limiting are enforced.

**Independent Test**: Register an account, log in with correct credentials → 200 with `accessToken`/`refreshToken`/`expiresAt`; wrong password → 401; exceed 10 failures → 429.

### Tests for User Story 2

> Test method naming: `MethodName_StateUnderTest_ExpectedBehavior` (e.g. `LoginAsync_CorrectCredentials_ReturnsAuthResponse`)

- [X] T036 [US2] Write failing unit tests for `AuthService.LoginAsync` in `tests/Librify.Tests/Services/AuthServiceTests.cs`: correct credentials → `AuthResponse`; wrong password → `InvalidCredentialsException`; unknown email → same `InvalidCredentialsException` (no enumeration); IsBlocked returns true → throws `RateLimitExceededException`; successful login resets failure counter via `ILoginAttemptTracker.Reset`

### Implementation for User Story 2

- [X] T038 [US2] Implement `AuthService.LoginAsync` in `src/Librify.Application/Auth/AuthService.cs`: normalise email; check `ILoginAttemptTracker.IsBlocked` — if blocked, throw `RateLimitExceededException`; find user by email (`FindByEmailAsync`); if not found, call `RecordFailure` and throw `InvalidCredentialsException`; verify password via `IPasswordHasher<User>.VerifyHashedPassword` — on failure, call `RecordFailure` and throw `InvalidCredentialsException`; on success, call `Reset`; generate access token via `IJwtTokenService`; generate refresh token (`Guid.NewGuid().ToString("N")`), create `RefreshToken` entity with new `FamilyId`, persist via `IRefreshTokenRepository`; return `AuthResponse`; use `ConfigureAwait(false)` on all awaited calls — depends on T034 (AuthService exists)
- [X] T039 [US2] Add `POST /api/auth/login` action to `src/Librify.Api/Controllers/AuthController.cs` with `[ProducesResponseType(200)]`, `[ProducesResponseType(400)]`, `[ProducesResponseType(401)]`, `[ProducesResponseType(429)]`; call `AuthService.LoginAsync` and return 200 `AuthResponse` on success; `InvalidCredentialsException → 401` and `RateLimitExceededException → 429` are both handled automatically by `GlobalExceptionHandler` — no try/catch needed in the controller — depends on T038

**Checkpoint**: `POST /api/auth/login` works end-to-end; returned JWT is valid; all User Story 2 tests pass

---

## Phase 5: User Story 3 — Refresh an Expired Access Token (Priority: P2)

**Goal**: A caller with a valid refresh token can obtain a new access token + rotated refresh token via `POST /api/auth/refresh`; reuse of a consumed token triggers full family invalidation.

**Independent Test**: Log in, exchange refresh token → 200 with new `accessToken` and new `refreshToken`; old refresh token is now revoked; presenting a consumed token → 401 and all family tokens revoked.

### Tests for User Story 3

> Test method naming: `MethodName_StateUnderTest_ExpectedBehavior` (e.g. `RefreshTokenAsync_ConsumedToken_RevokesFamily`)

- [X] T040 [US3] Write failing unit tests for `AuthService.RefreshTokenAsync` in `tests/Librify.Tests/Services/AuthServiceTests.cs`: valid token → new `AuthResponse` with rotated tokens; old token is revoked after rotation; expired token → `InvalidRefreshTokenException`; revoked token → `InvalidRefreshTokenException`; consumed (already-revoked) token → `RevokeByFamilyIdAsync` called + `InvalidRefreshTokenException`

### Implementation for User Story 3

- [X] T043 [US3] Implement `AuthService.RefreshTokenAsync` in `src/Librify.Application/Auth/AuthService.cs`: find token via `IRefreshTokenRepository.FindByTokenAsync`; if not found → throw `InvalidRefreshTokenException`; if `IsRevoked` is already `true` (reuse detection) → call `RevokeByFamilyIdAsync(token.FamilyId)` and throw `InvalidRefreshTokenException`; if expired → throw `InvalidRefreshTokenException`; revoke old token (`IsRevoked = true`, `SaveChangesAsync`); generate new access token; create new `RefreshToken` with same `FamilyId`; persist new token; return `AuthResponse`; use `ConfigureAwait(false)` on all awaited calls — depends on T026, T038
- [X] T044 [US3] Add `POST /api/auth/refresh` action to `src/Librify.Api/Controllers/AuthController.cs` with `[ProducesResponseType(200)]`, `[ProducesResponseType(400)]`, `[ProducesResponseType(401)]`; call `AuthService.RefreshTokenAsync`; return 200 `AuthResponse` — depends on T043

**Checkpoint**: Full token rotation works; reuse detection triggers family invalidation; all User Story 3 tests pass

---

## Phase 6: User Story 4 — Log Out (Priority: P2)

**Goal**: A user with a valid access token can revoke their refresh token via `POST /api/auth/logout`; the refresh token is permanently invalidated.

**Independent Test**: Log in, log out with refresh token → 204 No Content; attempt to use the same refresh token → 401.

### Tests for User Story 4

> Test method naming: `MethodName_StateUnderTest_ExpectedBehavior` (e.g. `LogoutAsync_ValidToken_RevokesRefreshToken`)

- [X] T045 [US4] Write failing unit tests for `AuthService.LogoutAsync` in `tests/Librify.Tests/Services/AuthServiceTests.cs`: valid refresh token → token is revoked; token not found → throw `InvalidRefreshTokenException`; already-revoked token → throw `InvalidRefreshTokenException`

### Implementation for User Story 4

- [X] T047 [US4] Implement `AuthService.LogoutAsync` in `src/Librify.Application/Auth/AuthService.cs`: find token via `IRefreshTokenRepository.FindByTokenAsync`; if not found or `IsRevoked` → throw `InvalidRefreshTokenException`; set `IsRevoked = true`; call `SaveChangesAsync`; use `ConfigureAwait(false)` on all awaited calls — depends on T043
- [X] T048 [US4] Add `POST /api/auth/logout` action to `src/Librify.Api/Controllers/AuthController.cs` with `[Authorize]` attribute, `[ProducesResponseType(204)]`, `[ProducesResponseType(400)]`, `[ProducesResponseType(401)]`; call `AuthService.LogoutAsync`; return `NoContent()` — depends on T047

**Checkpoint**: Logout correctly invalidates the refresh token; all User Story 4 tests pass

---

## Phase 7: Polish & Cross-Cutting Concerns

- [X] T049 Run full test suite `dotnet test` and fix all failures; ensure all four user story acceptance scenarios have passing test coverage
- [X] T050 [P] Apply EF migration to local PostgreSQL: `dotnet ef database update --project src/Librify.Infrastructure --startup-project src/Librify.Api`; run the API with `dotnet run --project src/Librify.Api` and validate all four endpoints via the quickstart.md scenarios

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately
- **Foundational (Phase 2)**: Depends on Phase 1 completion — **BLOCKS all user stories**
- **US1 (Phase 3)**: Depends on Phase 2 completion — no dependency on US2/US3/US4
- **US2 (Phase 4)**: Depends on Phase 2 + US1 (shares `AuthService.cs`) — implement after US1
- **US3 (Phase 5)**: Depends on Phase 2 + US2 (refresh requires a valid login flow) — implement after US2
- **US4 (Phase 6)**: Depends on Phase 2 + US2 (logout requires a token from login) — implement after US2; can parallel with US3
- **Polish (Phase 7)**: Depends on all desired stories being complete

### Within Each User Story

1. Write service unit tests (mark them failing)
2. Implement service method in `AuthService.cs`
3. Implement controller action in `AuthController.cs`
4. Verify tests pass

### Parallel Opportunities

**Phase 1**: T002 and T003 can run in parallel (different project files)

**Phase 2 Group A** (T006–T021): All touch different files — run all simultaneously

**Phase 2 Group B**: T023 and T024 can run in parallel (different files, both depend on T010/T011 and T022)

**Phase 2 Group C**: T029 can run in parallel with T025/T026 (Program.cs vs repository files)

**Per story**: Service unit test task runs first, then implementation tasks sequentially

---

## Parallel Example: Phase 2 Group A

```
Launch simultaneously (all different files):
  T006  User entity
  T007  RefreshToken entity
  T008  IUserRepository
  T009  IRefreshTokenRepository
  T010  IJwtTokenService
  T011  ILoginAttemptTracker
  T012  IAuthService
  T013  RegisterRequest DTO
  T014  LoginRequest DTO
  T015  RefreshTokenRequest DTO
  T016  LogoutRequest DTO
  T017  AuthResponse DTO
  T018  DuplicateEmailException
  T019  InvalidCredentialsException
  T020  InvalidRefreshTokenException
  T020b RateLimitExceededException

Then (depends on above):
  T021  AuthService skeleton
  T022  AppDbContext modification

Then (depends on T022):
  T023 + T024 in parallel  (JwtTokenService + LoginAttemptTracker)
  T025 + T026 in parallel  (UserRepository + RefreshTokenRepository)
```

## Example: User Story 1

```
First:
  T031  AuthService unit tests  → tests/Librify.Tests/Services/AuthServiceTests.cs

Then sequentially:
  T034  AuthService.RegisterAsync implementation
  T035  AuthController POST /api/auth/register
```

---

## Implementation Strategy

### MVP First (User Story 1 + 2 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational
3. Complete Phase 3: User Story 1 (Register)
4. Complete Phase 4: User Story 2 (Login)
5. **STOP and VALIDATE**: A user can register and log in with a working JWT
6. Demo/deploy if ready

### Incremental Delivery

1. Phase 1 + 2 → Foundation ready
2. Phase 3 → Registration works → test independently
3. Phase 4 → Login works (full auth flow) → **MVP milestone**
4. Phase 5 → Token refresh works → deploy/demo
5. Phase 6 → Logout works → full feature complete
6. Phase 7 → Polish and validate

---

## Notes

- Each task description contains enough context to implement without re-reading this file
- Commit after each completed phase using Conventional Commits (e.g. `feat(auth): implement user registration`)
- Never use `.Result` or `.Wait()` — all async code must use `await` with `ConfigureAwait(false)` in Infrastructure/Application
- Never commit `JwtSettings:Secret` — use `dotnet user-secrets` in development, environment variables in production
- Email normalisation (`ToLowerInvariant()`) must happen in both `RegisterAsync` and `LoginAsync` — not just at the repository layer
