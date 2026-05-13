# Quickstart: User Authentication

## New NuGet Packages

```bash
# Librify.Api
dotnet add src/Librify.Api/Librify.Api.csproj package Microsoft.AspNetCore.Authentication.JwtBearer --version 10.0.7

# Librify.Infrastructure
dotnet add src/Librify.Infrastructure/Librify.Infrastructure.csproj package Microsoft.Extensions.Identity.Core --version 10.0.7

# Test project (scaffold first — see below)
dotnet add tests/Librify.Tests/Librify.Tests.csproj package Microsoft.NET.Test.Sdk
dotnet add tests/Librify.Tests/Librify.Tests.csproj package xunit
dotnet add tests/Librify.Tests/Librify.Tests.csproj package xunit.runner.visualstudio
dotnet add tests/Librify.Tests/Librify.Tests.csproj package FluentAssertions
dotnet add tests/Librify.Tests/Librify.Tests.csproj package Moq
dotnet add tests/Librify.Tests/Librify.Tests.csproj package Microsoft.AspNetCore.Mvc.Testing --version 10.0.7
dotnet add tests/Librify.Tests/Librify.Tests.csproj package Microsoft.EntityFrameworkCore.Sqlite --version 10.0.7
```

## Scaffold Test Project

```bash
mkdir -p tests/Librify.Tests
dotnet new xunit -o tests/Librify.Tests --framework net10.0
dotnet sln add tests/Librify.Tests/Librify.Tests.csproj
dotnet add tests/Librify.Tests/Librify.Tests.csproj reference src/Librify.Api/Librify.Api.csproj
dotnet add tests/Librify.Tests/Librify.Tests.csproj reference src/Librify.Application/Librify.Application.csproj
dotnet add tests/Librify.Tests/Librify.Tests.csproj reference src/Librify.Infrastructure/Librify.Infrastructure.csproj
```

## JWT Configuration

**User Secrets (development — never commit the secret)**:

```bash
dotnet user-secrets set "JwtSettings:Secret" "your-super-secret-key-must-be-32-chars!" --project src/Librify.Api
```

**appsettings.json** (structure only — no secret value):

```json
"JwtSettings": {
  "Issuer": "librify-api",
  "Audience": "librify-clients",
  "AccessTokenExpiryMinutes": 15,
  "RefreshTokenExpiryDays": 7
}
```

In production, `JwtSettings__Secret` comes from an environment variable.

## EF Migration

```bash
dotnet ef migrations add AddUserAuthentication \
  --project src/Librify.Infrastructure \
  --startup-project src/Librify.Api

dotnet ef database update \
  --project src/Librify.Infrastructure \
  --startup-project src/Librify.Api
```

## Key Endpoints

| Method | Path | Auth Required | Description |
|--------|------|---------------|-------------|
| POST | `/api/auth/register` | None | Create a new account |
| POST | `/api/auth/login` | None | Exchange credentials for access + refresh token |
| POST | `/api/auth/refresh` | None | Rotate refresh token; receive new access token |
| POST | `/api/auth/logout` | Bearer | Revoke the current refresh token |
