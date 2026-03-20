# CLAUDE.md

This file provides guidance to AI assistants (Claude and others) working within this repository. Keep it up to date as the project evolves.

---

## Repository Overview

**Repository:** KrzysiekHan/teamstestrepo
**Status:** Newly initialized — no source code has been committed yet.

> Update this section with a short description of the project's purpose, architecture, and primary language/framework once development begins.

---

## Project Structure

> This section should be updated once source files are added. A typical structure might look like:

```
teamstestrepo/
├── CLAUDE.md           # AI assistant guidance (this file)
├── README.md           # Human-facing project documentation
├── src/                # Application source code
├── tests/              # Test suites
├── docs/               # Additional documentation
└── .github/            # GitHub Actions workflows / issue templates
```

---

## Tech Stack

> Fill in once the stack is decided. Example entries:

- **Language:** (e.g., TypeScript, Python, Go)
- **Framework:** (e.g., Next.js, FastAPI, Gin)
- **Database:** (e.g., PostgreSQL, SQLite, Redis)
- **Testing:** (e.g., Jest, pytest, Go test)
- **Linter/Formatter:** (e.g., ESLint + Prettier, Ruff, gofmt)

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
feat(auth): add JWT token refresh endpoint
fix(api): handle null pointer in user lookup
docs: update CLAUDE.md with project structure
```

### Pull Requests

- Keep PRs focused — one concern per PR
- Include a description of what changed and why
- Ensure all CI checks pass before requesting review
- Link related issues with `Closes #<issue-number>`

---

## Commands

> Update this section with actual commands once tooling is configured.

### Setup

```bash
# Clone and enter the repo
git clone <repo-url>
cd teamstestrepo

# Install dependencies (update command for your package manager)
# npm install / pip install -r requirements.txt / go mod download
```

### Development

```bash
# Start development server (update for your stack)
# npm run dev / uvicorn app.main:app --reload / go run ./cmd/server
```

### Testing

```bash
# Run all tests
# npm test / pytest / go test ./...

# Run tests in watch mode
# npm run test:watch / pytest -f / gotestsum --watch
```

### Linting & Formatting

```bash
# Lint
# npm run lint / ruff check . / golangci-lint run

# Format
# npm run format / ruff format . / gofmt -w .

# Type check
# npm run typecheck / mypy . / go vet ./...
```

### Build

```bash
# Production build
# npm run build / python -m build / go build ./...
```

---

## Code Conventions

> Establish and document conventions here as the codebase develops. Initial guidelines:

### General

- Prefer clarity over cleverness — write code that is easy to read and review
- Keep functions small and focused on a single responsibility
- Avoid premature abstractions; extract helpers when a pattern appears 3+ times
- No commented-out code in commits; use version control instead

### Naming

- Use descriptive names; avoid single-letter variables except in short loops
- Be consistent with the existing style in each file

### Error Handling

- Handle errors explicitly; do not silently swallow them
- Return meaningful error messages at system boundaries (API responses, CLI output)
- Log errors with enough context to diagnose them

### Security

- Never commit secrets, credentials, API keys, or tokens
- Use environment variables for configuration; provide a `.env.example` template
- Validate and sanitize all external input (user data, API payloads, file uploads)
- Follow OWASP Top 10 guidelines when building web interfaces

---

## Environment Variables

> Document required environment variables here. Example:

| Variable | Description | Required | Default |
|---|---|---|---|
| `APP_ENV` | Runtime environment (`development`, `production`) | Yes | `development` |
| `DATABASE_URL` | Connection string for the database | Yes | — |
| `SECRET_KEY` | Application secret for signing tokens | Yes | — |

Copy `.env.example` to `.env` and fill in values before running locally.

---

## Testing Guidelines

- Write tests alongside new features — do not defer them
- Aim for high coverage of business logic; avoid testing implementation details
- Use descriptive test names that explain the scenario and expected outcome
- Prefer integration tests for critical user flows; unit tests for complex logic

---

## AI Assistant Instructions

When working in this repository, follow these guidelines:

1. **Read before modifying** — always read a file before editing it
2. **Minimal changes** — make only the changes necessary to complete the task; avoid unrelated refactors
3. **No secrets** — never commit credentials, tokens, or private keys
4. **Test your changes** — run the test suite and linter before committing
5. **Clear commits** — write a commit message that explains *why*, not just *what*
6. **Update this file** — when you add significant new structure, commands, or conventions, update CLAUDE.md accordingly
7. **Ask before destructive actions** — confirm with the user before deleting files, force-pushing, or modifying CI/CD pipelines

---

## Useful References

> Add links to relevant documentation, architecture decisions, or external services here.

- [Conventional Commits](https://www.conventionalcommits.org/)
- [OWASP Top 10](https://owasp.org/www-project-top-ten/)

---

*Last updated: 2026-03-20 — repository initialization*
