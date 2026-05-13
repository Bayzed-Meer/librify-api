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
| Application | Mapster | 10.0.7 |
| Application | Mapster.DependencyInjection | 10.0.7 |
| Infrastructure | Microsoft.EntityFrameworkCore | 10.0.7 |
| Infrastructure | Microsoft.EntityFrameworkCore.Design | 10.0.7 |
| Infrastructure | Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore | 10.0.7 |
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

## DTO Pattern

All features follow a strict Request / Response / Mapper separation:

- `[Feature]Request` — HTTP input DTO; carries validation attributes; lives in `Application/[Feature]/DTOs/`
- `[Feature]Response` — HTTP output DTO; shaped for the client; lives in `Application/[Feature]/DTOs/`
- Domain entities MUST never be returned from a service or controller — always map to a Response DTO first

### Mapster Mapping

Mappings are defined as `IRegister` configuration classes in `Application/[Feature]/Mappers/`:

```csharp
public class AuthMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<User, RegisterResponse>()
            .Map(dest => dest.UserId, src => src.Id);
    }
}
```

- Register all configs via `services.AddMapster()` (scans assemblies automatically with `Mapster.DependencyInjection`)
- Inject `IMapper` into services — never call `Adapt<T>()` directly in services (use the injected mapper for testability)
- Prefer explicit field mappings in config — avoid implicit mapping of sensitive fields

### Folder Structure per Feature

```
Application/
└── [Feature]/
    ├── DTOs/
    │   ├── [Feature]Request.cs
    │   └── [Feature]Response.cs
    ├── Mappers/
    │   └── [Feature]MappingConfig.cs
    ├── I[Feature]Service.cs
    └── [Feature]Service.cs
```

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

Tests live in `tests/Librify.Tests/Services/`. Only service-level unit tests are written for this project — controller and repository tests are out of scope.

- xUnit + FluentAssertions + Moq
- Mock all dependencies; never touch a real database or HTTP pipeline in tests
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

## Agent Workflow (Spec-Driven Development)

This project follows [spec-kit SDD](https://github.com/github/spec-kit). Every feature goes through the full speckit loop:

| Step | Command | Purpose |
|------|---------|---------|
| 1 | `/speckit-constitution` | Establish or update project principles (once per project) |
| 2 | `/speckit-specify` | Create feature spec from a natural language description |
| 3 | `/speckit-clarify` | (optional) De-risk ambiguous areas before planning |
| 4 | `/speckit-git-feature` | Create a feature branch |
| 5 | `/speckit-plan` | Generate implementation plan from spec |
| 6 | `/speckit-checklist` | (optional) Validate requirements completeness |
| 7 | `/speckit-tasks` | Break plan into dependency-ordered tasks |
| 8 | `/speckit-analyze` | (optional) Cross-artifact consistency check |
| 9 | `/speckit-implement` | Execute all tasks |
| 10 | `/speckit-git-commit` | Commit changes with conventional commit message |
| 11 | `/speckit-git-remote` | Push branch and open GitHub PR |

Specs live in `.specify/specs/<feature-name>/`; run `specify check` to verify setup.

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


<!-- SPECKIT START -->
For additional context about technologies to be used, project structure,
shell commands, and other important information, read the current plan
at `specs/001-user-authentication/plan.md`.
<!-- SPECKIT END -->
