# CI/CD Setup Instructions

## Required Secrets

To enable automated Docker image publishing, you need to add a GitHub secret:

### DockerHub Access Token

1. **Create DockerHub Access Token**:
   - Go to https://hub.docker.com/settings/security
   - Click "New Access Token"
   - Name: `github-actions-hotwind`
   - Access permissions: Read, Write, Delete
   - Copy the token (shown only once)

2. **Add to GitHub Secrets**:
   - Go to your repository on GitHub
   - Navigate to Settings → Secrets and variables → Actions
   - Click "New repository secret"
   - Name: `DOCKERHUB_TOKEN`
   - Value: Paste the DockerHub access token
   - Click "Add secret"

### Verify Setup

After adding the secret, the workflow will automatically:
1. Build and test on every push to main
2. Calculate semantic version from commits
3. Build multi-architecture Docker images
4. Push to DockerHub as:
   - `number_27/hotwind-api:latest`
   - `number_27/hotwind-api:1.0.0`
   - `number_27/hotwind-api:1.0`
   - `number_27/hotwind-api:1`
   - `number_27/hotwind-cli:latest`
   - `number_27/hotwind-cli:1.0.0`
   - `number_27/hotwind-cli:1.0`
   - `number_27/hotwind-cli:1`
5. Create GitHub release with version tag
6. Update git tags

## Testing Locally

### Test Docker Builds

```bash
# Test API build
docker build -t hotwind-api:test -f src/HotWind.Api/Dockerfile .

# Test CLI build
docker build -t hotwind-cli:test -f src/HotWind.Cli/Dockerfile .

# Run API locally
docker run -p 8080:8080 \
  -e ConnectionStrings__DefaultConnection="Host=host.docker.internal;Database=hotwind;Username=hotwind_user;Password=hotwind_pass" \
  hotwind-api:test

# Run CLI locally
docker run -it \
  -e ApiSettings__BaseUrl="http://host.docker.internal:8080" \
  hotwind-cli:test
```

### Test Semantic Versioning Logic

```bash
# Simulate what the workflow will do
git log v1.0.0..HEAD --oneline

# Check for conventional commit types
git log v1.0.0..HEAD --oneline | grep -E "^[a-f0-9]+ (feat|fix|chore|docs)"
```

## Workflow Triggers

### Main Workflow (`build-and-release.yml`)

Triggers on:
- Push to `main` branch
- Pull requests to `main`

Actions:
- **On PR**: Build and test only (no version bump, no Docker push)
- **On main push**: Full workflow (build, test, version, Docker push, release)

### PR Check Workflow (`pr-check.yml`)

Triggers on:
- Pull requests to `main`

Actions:
- Lint commit messages (must follow conventional commits)
- Build and test
- Test Docker builds (no push)

## Semantic Versioning Rules

The workflow automatically calculates the next version based on commit messages:

| Commit Type | Example | Version Change |
|-------------|---------|----------------|
| `feat:` | `feat(api): add new endpoint` | 1.0.0 → 1.1.0 |
| `fix:` | `fix(cli): correct bug` | 1.0.0 → 1.0.1 |
| `feat!:` | `feat(api)!: breaking change` | 1.0.0 → 2.0.0 |
| `BREAKING CHANGE:` | Any commit with this footer | 1.0.0 → 2.0.0 |
| Other | `docs:`, `chore:`, etc. | 1.0.0 → 1.0.1 |

## Monitoring Builds

### GitHub Actions

- View workflow runs: https://github.com/YOUR_REPO/actions
- Check build logs for any failures
- Review published releases: https://github.com/YOUR_REPO/releases

### DockerHub

- View published images: https://hub.docker.com/u/number_27
- Check image sizes and layers
- Verify multi-architecture support (amd64, arm64)

### Docker Image Tags

Each successful build creates multiple tags:

```
number_27/hotwind-api:latest       # Always points to latest version
number_27/hotwind-api:1            # Latest 1.x.x
number_27/hotwind-api:1.0          # Latest 1.0.x
number_27/hotwind-api:1.0.0        # Specific version
```

For production use, always reference specific versions:
```yaml
image: number_27/hotwind-api:1.0.0
```

## Troubleshooting

### Build Fails: "Invalid credentials"

- Verify `DOCKERHUB_TOKEN` secret is set correctly
- Check token has not expired
- Ensure token has Read/Write permissions

### Build Fails: "manifest unknown"

- First build of the repository
- DockerHub repository doesn't exist yet
- Will be created automatically on first successful push

### Semantic Version Not Incrementing

- Check commit messages follow conventional commits format
- Ensure commits since last tag contain `feat:` or `fix:`
- View workflow logs for version calculation details

### Docker Build Fails: "No such file or directory"

- Check Dockerfile COPY paths are correct
- Verify .dockerignore isn't excluding required files
- Ensure build context is repository root

## Security Best Practices

1. **Rotate DockerHub tokens regularly** (every 90 days recommended)
2. **Use separate tokens** for different projects
3. **Review Dependabot PRs** for dependency updates
4. **Scan images** for vulnerabilities:
   ```bash
   docker scan number_27/hotwind-api:latest
   ```
5. **Never commit secrets** to the repository
6. **Use GitHub Advanced Security** if available

## Advanced Configuration

### Custom Docker Registry

To use a different registry (e.g., GitHub Container Registry):

1. Update `.github/workflows/build-and-release.yml`:
   ```yaml
   env:
     REGISTRY: ghcr.io
     DOCKERHUB_USERNAME: ${{ github.repository_owner }}
   ```

2. Add `GHCR_TOKEN` secret with GitHub Personal Access Token

### Custom Versioning Logic

Modify the `semantic-version` job in `build-and-release.yml` to customize version calculation logic.

### Build Notifications

Add Slack/Discord notifications:

```yaml
- name: Notify build status
  if: always()
  uses: 8398a7/action-slack@v3
  with:
    status: ${{ job.status }}
    webhook_url: ${{ secrets.SLACK_WEBHOOK }}
```

## Next Steps

1. Add `DOCKERHUB_TOKEN` secret to GitHub repository
2. Push changes to trigger first workflow run
3. Verify Docker images appear on DockerHub
4. Test pulling and running images
5. Set up Kubernetes deployment (see DOCKER.md)
