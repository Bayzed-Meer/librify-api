---
paths:
  - "**/*.Tests/**/*.cs"
  - "**/*Tests.cs"
  - "**/*Test.cs"
---

# Testing Rules

## Framework & Structure
- Use **xUnit** for all tests — not MSTest or NUnit
- Use **FluentAssertions** for assertions — not raw `Assert.*`
- Use **Moq** (or NSubstitute) for mocking — never hand-roll fakes in production test code
- One test class per production class; file name mirrors the class under test with `Tests` suffix
- Arrange / Act / Assert sections separated by a blank line

## Test Naming
- Method name format: `MethodName_StateUnderTest_ExpectedBehavior`
- Example: `GetBook_WhenBookDoesNotExist_Returns404`
- Use `[Fact]` for single-case tests, `[Theory]` + `[InlineData]` / `[MemberData]` for parameterized

## What to Test
- **Required**: every public method, every error/exception path, every branch in business logic
- **Required**: every EF Core repository method (use in-memory provider or SQLite for speed)
- **Required**: every controller action via `WebApplicationFactory` integration tests for happy path + error cases
- **Not required**: trivial property getters/setters with no logic, auto-generated code, framework internals

## Integration Tests (WebApplicationFactory)
- Use `WebApplicationFactory<Program>` for HTTP-level controller tests
- Replace external dependencies (DB, third-party APIs) with test doubles or in-memory equivalents
- Test the full HTTP stack: status code, response body shape, headers
- Do not mock `HttpContext` directly — use the full middleware pipeline

## Patterns
```csharp
// Arrange
var sut = new BookService(_mockRepo.Object, _mockLogger.Object);

// Act
var result = await sut.GetByIdAsync(bookId);

// Assert
result.Should().NotBeNull();
result!.Title.Should().Be("Expected Title");
```

## Mandatory Workflow
After writing implementation code, agents MUST:
1. Write xUnit tests for every new service method, repository method, and controller action
2. Run `dotnet test` and fix all failures before committing
3. Do NOT commit with failing or skipped tests
