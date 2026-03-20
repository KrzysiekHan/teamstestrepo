# CLAUDE.md

This file provides guidance to AI assistants (Claude and others) working within this repository. Keep it up to date as the project evolves.

---

## Repository Overview

**Repository:** KrzysiekHan/teamstestrepo
**Language:** C# / .NET
**Type:** ASP.NET Core Web API
**Status:** Newly initialized — no source code has been committed yet.

> Update this section with a short description of the API's purpose and domain once development begins.

---

## Project Structure

Typical ASP.NET Core Web API layout (update once files are added):

```
teamstestrepo/
├── CLAUDE.md                        # AI assistant guidance (this file)
├── README.md                        # Human-facing documentation
├── teamstestrepo.sln                # Solution file
├── src/
│   └── TeamstestRepo.Api/           # Main Web API project
│       ├── Controllers/             # API controllers
│       ├── Models/                  # Request/response DTOs
│       ├── Services/                # Business logic
│       ├── Data/                    # EF Core DbContext, repositories
│       ├── Middleware/              # Custom middleware
│       ├── appsettings.json         # Configuration
│       ├── appsettings.Development.json
│       └── Program.cs               # App entry point / DI composition root
└── tests/
    ├── TeamstestRepo.UnitTests/     # xUnit unit tests
    └── TeamstestRepo.IntegrationTests/ # Integration tests
```

---

## Tech Stack

- **Language:** C# (.NET 8 or later)
- **Framework:** ASP.NET Core Web API
- **ORM:** Entity Framework Core (update if using Dapper or other)
- **Testing:** xUnit + Moq + FluentAssertions
- **Linter/Formatter:** `dotnet format` (built-in)
- **API Docs:** Swagger / Scalar (via `Swashbuckle` or `Microsoft.AspNetCore.OpenApi`)

> Update these entries as tooling decisions are finalized.

---

## Development Workflow

### Branching Strategy

- **Main branch:** `main` (protected — do not push directly)
- **Feature branches:** `feature/<short-description>`
- **Bug fix branches:** `fix/<short-description>`
- **AI-assisted branches:** `claude/<task-description>-<session-id>`

### Commit Conventions

Use [Conventional Commits](https://www.conventionalcommits.org/) format:

```
<type>(<scope>): <short summary>

[optional body]
```

Common types: `feat`, `fix`, `docs`, `refactor`, `test`, `chore`, `ci`

Examples:
```
feat(users): add GET /users/{id} endpoint
fix(auth): correct JWT expiry validation
test(orders): add unit tests for OrderService
docs: update CLAUDE.md with project structure
```

### Pull Requests

- Keep PRs focused — one concern per PR
- Include a description of what changed and why
- Ensure all CI checks pass before requesting review
- Link related issues with `Closes #<issue-number>`

---

## Commands

### Setup

```bash
# Clone and enter the repo
git clone <repo-url>
cd teamstestrepo

# Restore NuGet packages
dotnet restore

# Apply database migrations (if using EF Core)
dotnet ef database update --project src/TeamstestRepo.Api
```

### Development

```bash
# Run the API locally
dotnet run --project src/TeamstestRepo.Api

# Run with hot reload
dotnet watch run --project src/TeamstestRepo.Api
```

### Testing

```bash
# Run all tests
dotnet test

# Run with detailed output
dotnet test --logger "console;verbosity=detailed"

# Run a specific test project
dotnet test tests/TeamstestRepo.UnitTests
```

### Linting & Formatting

```bash
# Check formatting
dotnet format --verify-no-changes

# Apply formatting
dotnet format

# Build with warnings as errors (good for CI)
dotnet build --warnaserror
```

### Build

```bash
# Debug build
dotnet build

# Release build
dotnet build -c Release

# Publish (self-contained)
dotnet publish -c Release -o ./publish
```

### Entity Framework Core

```bash
# Add a new migration
dotnet ef migrations add <MigrationName> --project src/TeamstestRepo.Api

# Apply migrations
dotnet ef database update --project src/TeamstestRepo.Api

# Revert last migration
dotnet ef migrations remove --project src/TeamstestRepo.Api
```

---

## Code Conventions

### Naming (Microsoft C# conventions)

- **Classes, methods, properties:** `PascalCase`
- **Local variables, parameters:** `camelCase`
- **Private fields:** `_camelCase` (underscore prefix)
- **Constants:** `PascalCase` (not `ALL_CAPS`)
- **Interfaces:** prefix with `I` — `IUserService`, `IRepository<T>`
- **Async methods:** suffix with `Async` — `GetUserAsync()`, `SaveAsync()`

### Project Structure Conventions

- One class per file; filename matches class name
- Controllers are thin — delegate all logic to services
- Services contain business logic; repositories handle data access
- DTOs (request/response models) live in `Models/` and are separate from domain entities

### Async / Await

- Always use `async`/`await` for I/O-bound operations (DB, HTTP, file)
- Never use `.Result` or `.Wait()` — this can cause deadlocks
- Use `CancellationToken` parameters on all async public methods

### Dependency Injection

- Register services in `Program.cs` (or extension methods grouped by feature)
- Prefer constructor injection; avoid service locator pattern
- Use appropriate lifetimes: `Singleton`, `Scoped`, `Transient`

### Error Handling

- Use a global exception-handling middleware or `IExceptionHandler` (ASP.NET Core 8+)
- Return RFC 7807 Problem Details for API errors (`Results.Problem(...)`)
- Do not swallow exceptions silently; log with enough context
- Use `ILogger<T>` for structured logging — avoid `Console.WriteLine`

### Security

- Never commit secrets, connection strings, or API keys — use `appsettings.Development.json` (git-ignored) or environment variables
- Use ASP.NET Core's built-in data protection and authentication middleware
- Validate all incoming DTOs with Data Annotations or FluentValidation
- Sanitize query parameters to prevent injection attacks
- Follow OWASP Top 10 guidelines

---

## Configuration & Secrets

- `appsettings.json` — non-sensitive defaults (checked into git)
- `appsettings.Development.json` — local overrides, should be **git-ignored**
- Environment variables override `appsettings` at runtime
- Use [.NET Secret Manager](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets) for local development secrets:

```bash
dotnet user-secrets init --project src/TeamstestRepo.Api
dotnet user-secrets set "ConnectionStrings:Default" "Server=..." --project src/TeamstestRepo.Api
```

### Common Configuration Keys

| Key | Description | Required |
|---|---|---|
| `ConnectionStrings:Default` | Database connection string | Yes |
| `Jwt:SecretKey` | Secret key for JWT signing | Yes |
| `Jwt:Issuer` | JWT issuer | Yes |
| `Jwt:Audience` | JWT audience | Yes |

---

## Testing Guidelines

- Use **xUnit** as the test framework
- Use **Moq** for mocking dependencies
- Use **FluentAssertions** for readable assertions (`result.Should().Be(...)`)
- Use `WebApplicationFactory<Program>` for integration tests against the full pipeline
- Name test methods: `MethodName_Scenario_ExpectedResult`
  - Example: `GetUser_UserDoesNotExist_Returns404`
- Do not test EF Core internals; use an in-memory database or test containers for integration tests

---

## AI Assistant Instructions

When working in this repository, follow these guidelines:

1. **Read before modifying** — always read a file before editing it
2. **Follow C# conventions** — PascalCase types, `_camelCase` private fields, `Async` suffix on async methods
3. **Keep controllers thin** — business logic belongs in services, not controllers
4. **Always use async/await** — never `.Result` or `.Wait()`
5. **No secrets in code** — use configuration/environment variables
6. **Validate inputs** — all controller action parameters should be validated
7. **Run tests before committing** — `dotnet test` must pass
8. **Minimal changes** — make only the changes necessary; avoid unrelated refactors
9. **Update this file** — when significant structure or conventions change, update CLAUDE.md
10. **Ask before destructive actions** — confirm before deleting migrations, dropping tables, or force-pushing

---

## Useful References

- [ASP.NET Core documentation](https://learn.microsoft.com/en-us/aspnet/core/)
- [EF Core documentation](https://learn.microsoft.com/en-us/ef/core/)
- [C# coding conventions (Microsoft)](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- [Conventional Commits](https://www.conventionalcommits.org/)
- [OWASP Top 10](https://owasp.org/www-project-top-ten/)

---

*Last updated: 2026-03-20 — C# / ASP.NET Core Web API project setup*
