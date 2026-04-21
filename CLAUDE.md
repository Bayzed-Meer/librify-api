# Librify.Api — Project Memory

## Project Overview

Librify is a .NET 10 clean-architecture REST API for managing a personal book library. Built with ASP.NET Core, EF Core 10, and PostgreSQL.

## Solution Layout

```
Librify.slnx
└── src/
    ├── Librify.Api            # ASP.NET Core entry point (port 5041 / 7176)
    ├── Librify.Application    # Use cases, services, DTOs, interfaces
    ├── Librify.Domain         # Entities, value objects, domain interfaces
    └── Librify.Infrastructure # EF Core, repositories, PostgreSQL
```

### Project References

```
Api → Application → Domain
Api → Infrastructure
Infrastructure → Domain
Infrastructure → Application
```

### NuGet Packages

| Project | Package | Version |
|---------|---------|---------|
| Api | Microsoft.AspNetCore.OpenApi | 10.0.3 |
| Infrastructure | Microsoft.EntityFrameworkCore | 10.0.6 |
| Infrastructure | Microsoft.EntityFrameworkCore.Design | 10.0.6 |
| Infrastructure | Npgsql.EntityFrameworkCore.PostgreSQL | 10.0.1 |

## Architecture

Clean Architecture with layered responsibilities:

- **Domain** — entities, value objects, domain interfaces (no dependencies)
- **Application** — use-case services, DTOs (records with `required`), interfaces; depends only on Domain
- **Infrastructure** — `AppDbContext`, repositories, EF migrations; implements Application interfaces
- **Api** — controllers, DI wiring in `Program.cs`, OpenAPI; thin layer delegating to Application

### Key Conventions

- File-scoped namespaces: `namespace Librify.Api.Controllers;`
- Explicit access modifiers on all members
- `async`/`await` everywhere — never `.Result` or `.Wait()`
- Records for DTOs with `required` properties
- `ILogger<T>` for all logging
- Throw specific exception types; let global middleware handle unhandled exceptions
- Global exception middleware must be registered in `Program.cs` before `MapControllers()`

## Database

- **Provider**: PostgreSQL via Npgsql
- **DbContext**: `Librify.Infrastructure.Data.AppDbContext`
- **Connection string** (appsettings.json): `Host=localhost;Port=5432;Database=librify;Username=postgres;Password=your_password`
- Use `AsNoTracking()` for read-only queries
- Never expose `DbContext` outside the Infrastructure layer

## API Design

- `[ApiController]` on every controller
- Return `ActionResult<T>` — not bare `T` or raw `IActionResult`
- `[ProducesResponseType]` for every expected status code
- Error responses via `Problem()` / `ValidationProblem()` — not custom error objects
- Validation attributes on request DTOs, never manual validation in controllers
- OpenAPI mapped at `/openapi/v1.json` (development only)

## DI Registration

- Register in `Program.cs` or dedicated extension methods per layer
- Scoped for EF/repositories, Singleton for stateless services, Transient for lightweight utilities

## Testing

No test projects exist yet. When adding tests, scaffold a `*.Tests` project mirroring source structure and add:
- xUnit + FluentAssertions + NSubstitute (or Moq)
- Controller tests via `WebApplicationFactory<Program>`
- Repository tests via EF SQLite in-memory provider
- Test naming: `MethodName_StateUnderTest_ExpectedBehavior`

## Commits

Follow Conventional Commits:

```
feat(books): add book search endpoint
fix(auth): handle null user on token refresh
refactor(db): extract repository base class
test(books): add WebApplicationFactory integration tests
chore: update EF Core to 10.0.6
```

- Never `--no-verify`
- No `Co-Authored-By` trailers

## Agent Workflow

The full development loop uses these commands in order:

| Step | Command / Agent | Purpose |
|------|----------------|---------|
| 1 | `/spec` | Define feature spec with user interview |
| 2 | `git checkout -b feat/<name>` | Create feature branch from up-to-date `main` |
| 3 | `/feature-dev` | Plan + implement with codebase context |
| 4 | `test-driven-development` skill | Write tests before implementation |
| 5 | `dotnet-validator` agent | Build + test check |
| 6 | `/code-review` | Multi-agent review before PR |
| 7 | `/commit` | Conventional commit with checks |
| 8 | `/create-pr` | Push branch + open GitHub PR |
| 9 | CI + merge | GitHub Actions validates; merge when green |

## Development Commands

```bash
# Run API
dotnet run --project src/Librify.Api

# Hot-reload development
dotnet watch run --project src/Librify.Api

# Run tests
dotnet test

# Build solution
dotnet build
```

