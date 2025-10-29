# Contributing to HotWind

Thank you for your interest in contributing to HotWind!

## Commit Message Format

This project uses [Conventional Commits](https://www.conventionalcommits.org/) for automated semantic versioning.

### Format

```
<type>(<scope>): <description>

[optional body]

[optional footer(s)]
```

### Types

- **feat**: A new feature (triggers MINOR version bump)
- **fix**: A bug fix (triggers PATCH version bump)
- **docs**: Documentation only changes
- **style**: Changes that don't affect code meaning (whitespace, formatting)
- **refactor**: Code change that neither fixes a bug nor adds a feature
- **perf**: Performance improvement
- **test**: Adding or updating tests
- **build**: Changes to build system or dependencies
- **ci**: Changes to CI configuration
- **chore**: Other changes that don't modify src or test files
- **revert**: Reverts a previous commit

### Breaking Changes

Add `!` after type/scope or include `BREAKING CHANGE:` in footer to trigger MAJOR version bump:

```
feat(api)!: change invoice creation endpoint signature

BREAKING CHANGE: invoice endpoint now requires customer ID in request body
```

### Examples

```
feat(cli): add interactive customer search
fix(api): correct FIFO inventory deduction logic
docs: update README with Docker instructions
test(services): add exchange rate service tests
chore(deps): update Npgsql to 9.0.2
ci: add Docker build caching to workflow
```

### Scope

Scope is optional but recommended. Common scopes:
- `api` - HotWind.Api changes
- `cli` - HotWind.Cli changes
- `db` - Database schema changes
- `deps` - Dependency updates
- `docker` - Dockerfile changes
- `docs` - Documentation changes

## Pull Request Process

1. **Fork the repository** and create a feature branch
2. **Write conventional commits** following the format above
3. **Add tests** for new features or bug fixes
4. **Update documentation** if needed
5. **Ensure CI passes**:
   - Build succeeds
   - All tests pass
   - Commit messages are valid
   - Docker images build successfully
6. **Request review** from maintainers

## Development Workflow

### Local Development

```bash
# Create feature branch
git checkout -b feat/your-feature

# Make changes and commit
git add .
git commit -m "feat(api): add new feature"

# Push to your fork
git push origin feat/your-feature

# Open pull request
gh pr create
```

### Running Tests

```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test tests/HotWind.Api.Tests/

# Run with coverage
dotnet test /p:CollectCoverage=true
```

### Building Docker Images Locally

```bash
# Build API image
docker build -t hotwind-api:local -f src/HotWind.Api/Dockerfile .

# Build CLI image
docker build -t hotwind-cli:local -f src/HotWind.Cli/Dockerfile .

# Run API container
docker run -p 8080:8080 \
  -e ConnectionStrings__DefaultConnection="Host=host.docker.internal;Database=hotwind;..." \
  hotwind-api:local
```

## Versioning

This project uses **Semantic Versioning** (SemVer):

- **MAJOR**: Breaking changes (`feat!:` or `BREAKING CHANGE:`)
- **MINOR**: New features (`feat:`)
- **PATCH**: Bug fixes (`fix:`) and other changes

Version numbers are automatically calculated and tagged by CI/CD based on conventional commits.

## Code Standards

### C# Style

- Follow [Microsoft C# Coding Conventions](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- Use `var` for local variables when type is obvious
- Prefer explicit types for public APIs
- Use nullable reference types (`#nullable enable`)
- Add XML documentation for public APIs

### SQL Style

- Use uppercase for SQL keywords
- Use snake_case for table and column names
- Add comments for complex queries
- Use CTEs for readability

### Docker Best Practices

- Use multi-stage builds
- Run as non-root user
- Use Alpine base images for smaller size
- Include health checks
- Use .dockerignore

## Security

### Reporting Vulnerabilities

Please report security vulnerabilities to the maintainers privately via GitHub Security Advisories.

### Security Best Practices

- Never commit secrets or credentials
- Use parameterized SQL queries
- Validate all user input
- Follow OWASP guidelines

## Questions?

Open an issue or start a discussion on GitHub!
