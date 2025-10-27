# Docker Deployment Guide

This guide covers deploying HotWind components using Docker containers.

## Docker Images

Pre-built images are available on Docker Hub:

- **API**: `number_27/hotwind-api:latest`
- **CLI**: `number_27/hotwind-cli:latest`

Images are multi-architecture (amd64, arm64) and follow security best practices:
- Alpine-based for minimal size
- Non-root user (UID 1000)
- No unnecessary packages
- Multi-stage builds
- Health checks (API only)

## Running the API

### Basic Usage

```bash
docker run -p 8080:8080 \
  -e ConnectionStrings__DefaultConnection="Host=postgres;Database=hotwind;Username=hotwind_user;Password=hotwind_pass" \
  number_27/hotwind-api:latest
```

### With Docker Compose

Create `docker-compose.yml`:

```yaml
version: '3.8'

services:
  postgres:
    image: postgres:17-alpine
    environment:
      POSTGRES_DB: hotwind
      POSTGRES_USER: hotwind_user
      POSTGRES_PASSWORD: hotwind_pass
    volumes:
      - postgres-data:/var/lib/postgresql/data
      - ./scripts/schema.sql:/docker-entrypoint-initdb.d/01-schema.sql
      - ./scripts/seed-data.sql:/docker-entrypoint-initdb.d/02-seed-data.sql
    ports:
      - "5432:5432"
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U hotwind_user -d hotwind"]
      interval: 10s
      timeout: 5s
      retries: 5

  api:
    image: number_27/hotwind-api:latest
    environment:
      ConnectionStrings__DefaultConnection: "Host=postgres;Database=hotwind;Username=hotwind_user;Password=hotwind_pass"
      ASPNETCORE_ENVIRONMENT: Production
    ports:
      - "8080:8080"
    depends_on:
      postgres:
        condition: service_healthy
    healthcheck:
      test: ["CMD-SHELL", "wget --no-verbose --tries=1 --spider http://localhost:8080/health || exit 1"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 40s

volumes:
  postgres-data:
```

Start services:

```bash
docker-compose up -d
```

Check health:

```bash
docker-compose ps
curl http://localhost:8080/health
```

View logs:

```bash
docker-compose logs -f api
```

## Running the CLI

### Interactive Mode

```bash
docker run -it \
  -e ApiSettings__BaseUrl="http://host.docker.internal:8080" \
  number_27/hotwind-cli:latest
```

### One-off Commands (Kubernetes CronJob)

For running specific operations in Kubernetes:

```bash
# The CLI will need to be extended to support command-line arguments
# for non-interactive execution in Kubernetes CronJobs
docker run \
  -e ApiSettings__BaseUrl="http://api-service:8080" \
  number_27/hotwind-cli:latest \
  --command generate-rates --start-date 2024-01-01 --end-date 2024-12-31
```

**Note**: Current CLI implementation is interactive only. For Kubernetes CronJobs, you would need to:
1. Add command-line argument parsing
2. Support non-interactive mode
3. Return appropriate exit codes

## Kubernetes Deployment Considerations

### Security Context

Both images run as non-root user (UID 1000):

```yaml
securityContext:
  runAsNonRoot: true
  runAsUser: 1000
  runAsGroup: 1000
  allowPrivilegeEscalation: false
  capabilities:
    drop:
      - ALL
  readOnlyRootFilesystem: true
```

### Resource Limits

Recommended resource settings:

```yaml
# API
resources:
  requests:
    memory: "256Mi"
    cpu: "250m"
  limits:
    memory: "512Mi"
    cpu: "500m"

# CLI (CronJob)
resources:
  requests:
    memory: "128Mi"
    cpu: "100m"
  limits:
    memory: "256Mi"
    cpu: "250m"
```

### Health Checks

API health check endpoint:

```yaml
livenessProbe:
  httpGet:
    path: /health
    port: 8080
  initialDelaySeconds: 15
  periodSeconds: 20

readinessProbe:
  httpGet:
    path: /health
    port: 8080
  initialDelaySeconds: 5
  periodSeconds: 10
```

### Configuration

Use ConfigMaps and Secrets:

```yaml
# ConfigMap for non-sensitive config
apiVersion: v1
kind: ConfigMap
metadata:
  name: hotwind-config
data:
  ASPNETCORE_ENVIRONMENT: "Production"
  ApiSettings__BaseUrl: "http://hotwind-api:8080"

---
# Secret for database connection
apiVersion: v1
kind: Secret
metadata:
  name: hotwind-db
type: Opaque
stringData:
  connection-string: "Host=postgres;Database=hotwind;Username=hotwind_user;Password=..."
```

Reference in deployment:

```yaml
env:
  - name: ConnectionStrings__DefaultConnection
    valueFrom:
      secretKeyRef:
        name: hotwind-db
        key: connection-string
  - name: ASPNETCORE_ENVIRONMENT
    valueFrom:
      configMapKeyRef:
        name: hotwind-config
        key: ASPNETCORE_ENVIRONMENT
```

### Network Policies

The API needs:
- Ingress: from Ingress Controller on port 8080
- Egress: to PostgreSQL on port 5432

The CLI needs:
- Egress: to API on port 8080

### Persistence

For PostgreSQL in Kubernetes:
- Use StatefulSet with persistent volumes
- Configure backup strategy
- Consider using managed PostgreSQL (CloudSQL, RDS, Azure Database)

## Image Versions

Images are tagged with semantic versions:

- `latest` - Latest stable release
- `1` - Latest 1.x.x version
- `1.0` - Latest 1.0.x version
- `1.0.0` - Specific version

For production, always use specific version tags:

```yaml
image: number_27/hotwind-api:1.0.0
```

## Building Custom Images

### Local Build

```bash
# Build API
docker build -t hotwind-api:custom -f src/HotWind.Api/Dockerfile .

# Build CLI
docker build -t hotwind-cli:custom -f src/HotWind.Cli/Dockerfile .
```

### Multi-platform Build

```bash
docker buildx create --use
docker buildx build --platform linux/amd64,linux/arm64 \
  -t hotwind-api:multi -f src/HotWind.Api/Dockerfile .
```

## Environment Variables

### API

| Variable | Description | Default |
|----------|-------------|---------|
| `ConnectionStrings__DefaultConnection` | PostgreSQL connection string | Required |
| `ASPNETCORE_URLS` | Listen URLs | `http://+:8080` |
| `ASPNETCORE_ENVIRONMENT` | Environment name | `Production` |
| `Logging__LogLevel__Default` | Log level | `Information` |

### CLI

| Variable | Description | Default |
|----------|-------------|---------|
| `ApiSettings__BaseUrl` | API base URL | `http://localhost:5280` |

## Troubleshooting

### API won't start

Check database connectivity:

```bash
docker exec -it <api-container> sh
wget -O- http://localhost:8080/health
```

View logs:

```bash
docker logs <api-container>
```

### Permission denied errors

Images run as UID 1000. Ensure mounted volumes have correct permissions:

```bash
chown -R 1000:1000 /path/to/volume
```

### CLI cannot connect to API

From inside CLI container, test API connectivity:

```bash
docker exec -it <cli-container> sh
wget -O- http://api:8080/health
```

Check network configuration:

```bash
docker network inspect <network-name>
```

## Performance Tuning

### .NET Garbage Collection

For containerized workloads:

```yaml
env:
  - name: DOTNET_gcServer
    value: "1"
  - name: DOTNET_GCHeapHardLimit
    value: "0x20000000"  # 512MB
```

### Connection Pooling

Configure Npgsql connection pooling:

```
Host=postgres;Database=hotwind;Username=user;Password=pass;
Minimum Pool Size=0;Maximum Pool Size=20;Connection Idle Lifetime=300
```

## Security Best Practices

1. **Use secrets management**: Never hardcode credentials
2. **Scan images**: Use `docker scan` or Trivy
3. **Update regularly**: Apply security patches via Dependabot
4. **Run as non-root**: Already configured in images
5. **Use specific tags**: Avoid `latest` in production
6. **Enable read-only filesystem**: Add volumes for writable paths if needed
7. **Network segmentation**: Use Kubernetes NetworkPolicies

## Monitoring

### Prometheus Metrics

Consider adding prometheus-net for metrics:

```bash
curl http://localhost:8080/metrics
```

### Logging

Logs are written to stdout/stderr and can be collected by:
- Kubernetes: Fluentd, Fluent Bit
- Docker Compose: Loki, ELK stack

### Tracing

For distributed tracing, consider adding OpenTelemetry instrumentation.
