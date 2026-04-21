# Librify.Api — Agent-Driven Development Workflow

## Overview

Each feature follows a 7-step pipeline. Always work on a dedicated branch — never commit directly to `main`.

---

## Step 1 — Write the Feature Spec

```
/spec <feature-name>
```

Claude interviews you with 6 questions:

1. User story
2. Acceptance criteria
3. API / data contracts
4. Edge cases & error states
5. Out of scope
6. Success metrics

Output: `docs/specs/<feature-name>.spec.md`

At the end Claude presents an approval gate — choose **(a) proceed**, **(b) revise**, or **(c) stop**.

---

## Step 2 — Create a Feature Branch

```bash
git checkout -b feat/<feature-name>
```

Always branch from an up-to-date `main`:

```bash
git checkout main && git pull && git checkout -b feat/<feature-name>
```

---

## Step 3 — Implement the Feature

```
/feature-dev
```

Point it at the spec file when prompted: `docs/specs/<feature-name>.spec.md`

The agent will:

1. Explore the codebase and propose an architecture — **you approve before any code is written**
2. Implement the feature across all layers:
   - **Domain** — entities, value objects
   - **Application** — use-case services, DTOs, interfaces
   - **Infrastructure** — EF Core repositories
   - **Api** — controllers, DI registration
3. Write xUnit tests alongside each new class (services, repositories, controllers)
4. Run `dotnet test` → fix any failures → re-run until green
5. Run `dotnet build` → resolve all warnings and errors

The agent does **not** commit — that is Step 5.

---

## Step 4 — Validate Build & Tests

The `dotnet-validator` agent runs automatically at the end of `/feature-dev`, but you can invoke it explicitly:

```
Agent: dotnet-validator
```

It checks:
- `dotnet build` — zero errors, zero warnings
- `dotnet test` — all tests pass

Do not proceed to review until this agent reports all green.

---

## Step 5 — Code Review

```
/code-review
```

Six specialized review agents check the changes in parallel:

- Architecture & clean architecture conventions
- REST API design (endpoints, status codes, OpenAPI)
- EF Core & database patterns (queries, concurrency)
- Test coverage quality (unit + integration)
- Security & edge cases (input validation, auth, SQL injection)
- Type design & domain model correctness

The agent applies fixes automatically. You approve the final diff.

---

## Step 6 — Commit

```
/commit
```

This will:

1. Run `dotnet build` to confirm the tree is clean
2. Run `dotnet test` to confirm all tests pass
3. Create the commit following Conventional Commits format: `feat(books): add book search endpoint`

If the build or tests fail, the agent fixes the issue and creates a **new** commit — it never uses `--no-verify`.

---

## Step 7 — Push and Open a PR

```
/create-pr
```

This will:

1. Push the branch to GitHub
2. Open a pull request against `main` with a summary and test plan

---

## Step 8 — CI Validates (Automatic)

GitHub Actions runs two jobs automatically on every push:

| Job | What it checks |
|-----|---------------|
| `build` | `dotnet build` → `dotnet test` (all projects) |

Branch protection on `main` blocks merge until the job is green.

---

## Step 9 — Merge

Once CI is green, merge the PR on GitHub. Then locally:

```bash
git checkout main && git pull
```

Repeat from Step 1 for the next feature.

---

## Quick Reference

```
/spec <name>             →  interview → docs/specs/<name>.spec.md
git checkout -b feat/<name>
/feature-dev             →  implement (all layers) + xUnit tests + fix locally
dotnet-validator agent   →  build + test
/code-review             →  automated parallel review + fixes
/commit                  →  build + test + conventional commit
/create-pr               →  push + open PR
                             CI runs automatically
                             merge on GitHub when green
git checkout main && git pull
```

---

## Rules That Govern Agent Behavior

| Rule file | Covers |
|-----------|--------|
| `.claude/rules/dotnet.md` | C# style, ASP.NET Core, EF Core, error handling |
| `.claude/rules/testing.md` | xUnit, FluentAssertions, WebApplicationFactory |
| `.claude/rules/commits.md` | Conventional Commits format |
