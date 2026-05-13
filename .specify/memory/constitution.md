<!--
SYNC IMPACT REPORT
==================
Version change: (template) → 1.0.0
Modified principles: N/A (initial ratification)
Added sections: Technology Stack, Development Workflow, Quality Gates
Removed sections: N/A
Templates updated:
  ✅ .specify/templates/plan-template.md — Constitution Check gates already reference this file dynamically; no structural changes required
  ✅ .specify/templates/spec-template.md — No principle-driven mandatory sections added; template remains valid
  ✅ .specify/templates/tasks-template.md — Test tasks are OPTIONAL per template; constitution mandates service unit tests only — mark service tests as required, omit controller/repository tests when generating tasks for this project
  ✅ .specify/templates/checklist-template.md — Generic; no changes required
Follow-up TODOs: none — all placeholders resolved
-->

# Librify.Api Constitution

## Core Principles

### I. Clean Architecture Boundaries (NON-NEGOTIABLE)

The dependency rule is absolute and must never be violated regardless of convenience.

- **Domain**: entities, value objects, and domain interfaces only — no dependencies on any other layer.
- **Application**: use-case services, DTOs (records with `required` properties), and Application-layer interfaces — depends only on Domain.
- **Infrastructure**: `AppDbContext`, EF Core migrations, repository implementations — depends on Domain and Application. `DbContext` MUST NOT be exposed outside this layer.
- **Api**: thin controllers, DI registration, OpenAPI config — depends on Application and Infrastructure for wiring only. No business logic permitted here.

Violations of these rules are never acceptable, even for short-term convenience.

### II. Async-First (NON-NEGOTIABLE)

Every I/O operation MUST use `async`/`await`. Never use `.Result`, `.Wait()`, or blocking Task continuations — they cause deadlocks in ASP.NET Core.

- Use `ConfigureAwait(false)` in Infrastructure.
- `ConfigureAwait(false)` is not required in the Api and Application layer.
- No exceptions to this rule under any circumstances.

### III. API Contract Standards (NON-NEGOTIABLE)

All API responses MUST conform to the following:

- Error responses MUST use `Problem()` or `ValidationProblem()` — no custom error objects.
- Input validation MUST use data annotation attributes on DTOs — never manual `if`-checks in controllers.
- Every controller action MUST declare `[ProducesResponseType]` for every expected status code.
- OpenAPI is mapped at `/openapi/v1.json` in development only.
- All controllers MUST carry `[ApiController]` and return `ActionResult<T>`.

### IV. Observability and Error Handling

Every class that performs I/O or business logic MUST inject `ILogger<T>` — `Console.WriteLine` is never acceptable in production code.

- Throw specific, meaningful exception types — never `throw new Exception(...)`.
- Unhandled exceptions MUST propagate to the global exception middleware (`UseExceptionHandler` registered in `Program.cs` before `MapControllers()`).
- Never swallow exceptions in a `catch` block without logging.

### V. Test Coverage is Non-Negotiable

Every public Application-layer service method MUST have unit tests before a feature is considered complete. Controller and repository tests are explicitly out of scope for this project.

- Framework: xUnit + FluentAssertions + Moq.
- Mock all dependencies — never touch a real database or HTTP pipeline in tests.
- Tests live in `tests/Librify.Tests/Services/`.
- Test naming format: `MethodName_StateUnderTest_ExpectedBehavior`.
- No commits with failing or skipped tests are permitted.

When generating tasks for this project, service unit test tasks are **required** — controller and repository test tasks are not generated.

### VI. DTO and Mapping Standards (NON-NEGOTIABLE)

Domain entities MUST never cross the Application → Api boundary. Every feature uses the following DTO shape:

- `[Feature]Request` — HTTP input DTO with validation attributes.
- `[Feature]Response` — HTTP output DTO shaped for the client.
- Domain entities are mapped to Response DTOs inside the Application service before returning.
- Mappings are defined as `IRegister` classes (Mapster) in `Application/[Feature]/Mappers/`.
- Services MUST inject `IMapper` — never call `.Adapt<T>()` directly (breaks testability).
- Sensitive entity fields (passwords, hashes, internal IDs) MUST use explicit mapping config to prevent accidental exposure.

### VII. Simplicity and Explicitness

Explicit is always preferred over implicit:

- Explicit access modifiers on all members.
- File-scoped namespaces: `namespace Librify.X.Y;`.
- Use `var` only when the type is obvious from the right-hand side.
- Records with `required` properties for all DTOs.
- No premature abstractions — three similar lines are better than a wrong abstraction.
- YAGNI: do not build for hypothetical future requirements.
- Default to writing no comments; add one only when the _why_ is non-obvious.

## Technology Stack

- **Runtime**: .NET 10, C# with latest language features enabled
- **Web**: ASP.NET Core; minimal API wiring in `Program.cs`
- **ORM**: EF Core 10 via Npgsql (PostgreSQL); `AsNoTracking()` is mandatory for all read-only queries
- **Database**: PostgreSQL — `localhost:5432`, database `librify`
- **Mapping**: Mapster 10.0.7 — all Entity → DTO mappings defined as `IRegister` config classes in `Application/[Feature]/Mappers/`; inject `IMapper` into services; never use `Adapt<T>()` directly
- **DI lifetimes**: Scoped for EF/repositories; Singleton for stateless services; Transient for lightweight utilities
- **OpenAPI**: `Microsoft.AspNetCore.OpenApi` 10.0.3
- **Testing**: xUnit, FluentAssertions, Moq (service unit tests only)

## Development Workflow

All features follow the Spec-Kit SDD pipeline in strict order:

1. `/speckit-git-feature` — create a numbered feature branch
2. `/speckit-specify` — write the spec (WHAT, not HOW — no tech stack in specs)
3. `/speckit-clarify` — resolve ambiguities (optional but recommended)
4. `/speckit-plan` — generate implementation plan, data model, and contracts
5. `/speckit-tasks` — generate dependency-ordered task list
6. `/speckit-analyze` — cross-artifact consistency check before coding
7. `/speckit-implement` — execute all tasks
8. `/speckit-git-commit` — commit following Conventional Commits (no `--no-verify`, no `Co-Authored-By`)
9. `/speckit-git-remote` — push branch and open GitHub PR
10. CI must pass (`dotnet build` + `dotnet test`) before merge to `main`

## Quality Gates

- `dotnet build` must pass with zero warnings (warnings are treated as errors).
- `dotnet test` must pass with zero failures before any commit.
- GitHub Actions CI runs on every push to `main` and every pull request targeting `main`.
- No force-push to `main`.
- Commit messages MUST follow Conventional Commits: `feat(scope):`, `fix(scope):`, `refactor(scope):`, `test(scope):`, `chore:`, `docs:`.

## Governance

This constitution supersedes all other conventions in the project. Any conflict between this document and other guidelines MUST be resolved by amending the constitution — never by silent exception.

- Amendments require updating this file with an incremented version and a new `LAST_AMENDED_DATE`.
- Version bumps follow semantic versioning:
  - **MAJOR**: backward-incompatible principle removals or redefinitions.
  - **MINOR**: new principle or section added, or materially expanded guidance.
  - **PATCH**: clarifications, wording fixes, non-semantic refinements.
- All pull requests and reviews must verify compliance with the active constitution version.
- Complexity violations (e.g., extra layers, non-standard patterns) MUST be justified in the plan's Complexity Tracking table before implementation.

**Version**: 1.1.0 | **Ratified**: 2026-05-05 | **Last Amended**: 2026-05-12
