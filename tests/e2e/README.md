# HotWind E2E Test Suite

Comprehensive end-to-end tests for the HotWind API using [Grafana k6](https://grafana.com/docs/k6/latest/).

## Overview

The e2e test suite validates the HotWind API with two complementary approaches:

- **Functional Tests** (`functional-tests.js`): Validates API correctness, business logic, and error handling
- **Load Tests** (`load-tests.js`): Verifies performance under light concurrent load

## Prerequisites

### Required

1. **PostgreSQL 17** database initialized with HotWind schema and seed data
2. **HotWind API** running and accessible
3. **k6** for running tests locally

### Installing k6

**macOS:**
```bash
brew install k6
```

**Linux:**
```bash
sudo gpg -k
sudo gpg --no-default-keyring --keyring /usr/share/keyrings/k6-archive-keyring.gpg \
  --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys C5AD17C747E3415A3642D57D77C6C491D6AC1D69
echo "deb [signed-by=/usr/share/keyrings/k6-archive-keyring.gpg] https://dl.k6.io/deb stable main" | \
  sudo tee /etc/apt/sources.list.d/k6.list
sudo apt-get update
sudo apt-get install k6
```

**Windows:**
```bash
choco install k6
```

**Docker (no installation needed):**
```bash
docker run --rm grafana/k6:1.3.0 version
```

## Quick Start

### 1. Set Up Database

```bash
# Create database and user
psql -U postgres -c "CREATE DATABASE hotwind;"
psql -U postgres -c "CREATE USER hotwind_user WITH PASSWORD 'hotwind_pass';"
psql -U postgres -c "GRANT ALL PRIVILEGES ON DATABASE hotwind TO hotwind_user;"

# Initialize schema
psql -U hotwind_user -d hotwind -f scripts/schema.sql

# Load seed data
psql -U hotwind_user -d hotwind -f scripts/seed-data.sql
```

### 2. Start the API

```bash
cd src/HotWind.Api
dotnet run
```

Verify the API is running:
```bash
curl http://localhost:5280/health
# Should return: {"status":"healthy","timestamp":"..."}
```

### 3. Run Tests

**Functional tests:**
```bash
k6 run tests/e2e/functional-tests.js
```

**Load tests:**
```bash
k6 run tests/e2e/load-tests.js
```

**Against a different API URL:**
```bash
API_BASE_URL=http://192.168.1.100:8080 k6 run tests/e2e/functional-tests.js
```

**Using Docker:**
```bash
docker run --rm --network host \
  -v $(pwd)/tests/e2e:/tests \
  -e API_BASE_URL=http://localhost:5280 \
  grafana/k6:1.3.0 \
  run /tests/functional-tests.js
```

## Test Structure

### Functional Tests (`functional-tests.js`)

**Purpose:** Validate API correctness and business logic

**Execution:**
- 1 Virtual User (VU)
- 1 Iteration
- Sequential test execution

**Test Groups:**
1. **Health Check** - Validates `/health` endpoint
2. **Models API** - CRUD operations, search, filtering, error cases
3. **Customers API** - CRUD operations, search, error cases
4. **Exchange Rates** - Rate retrieval, generation, same-currency handling
5. **Invoices** - Creation with FIFO inventory deduction, retrieval, validation
6. **Reports** - Complex SQL queries (stock, price-list, currency-translation)
7. **Error Handling** - 4xx/5xx status code verification

**Coverage:**
- ✅ 15+ API endpoints
- ✅ Response structure validation (ApiResponse wrapper)
- ✅ Business logic verification (FIFO, exchange rates)
- ✅ Error handling (400 Bad Request, 404 Not Found)
- ✅ Search and filtering functionality
- ✅ Complex aggregation queries

**Thresholds:**
- HTTP error rate < 1%
- p95 response time < 2000ms

### Load Tests (`load-tests.js`)

**Purpose:** Verify performance under concurrent load

**Execution:**
- 10 Virtual Users (VUs)
- 30 second duration
- Random endpoint selection with 100ms think time

**Read-Only Endpoints Tested:**
- `/health`
- `/api/models?inStockOnly=true`
- `/api/models?search=bosch`
- `/api/customers`
- `/api/exchangerates/USD/UAH`
- `/api/exchangerates/EUR/UAH`
- `/api/reports/stock`
- `/api/invoices?limit=20`

**Thresholds:**
- HTTP error rate < 1%
- p95 response time < 2000ms
- p99 response time < 3000ms
- Throughput > 10 req/sec

## Test Results

Results are saved to `tests/e2e/results/`:

| File | Description |
|------|-------------|
| `functional-summary.json` | Complete k6 metrics, checks, and thresholds for functional tests |
| `functional-junit.xml` | JUnit format for CI integration (functional tests) |
| `load-summary.json` | Complete k6 metrics for load tests |
| `load-junit.xml` | JUnit format for CI integration (load tests) |

### Interpreting Results

**Console Output:**
```
✓ health returns 200
✓ models list has data array
✗ invalid SKU returns 404
  ↳  0% — ✓ 0 / ✗ 1

http_req_duration..............: avg=245ms  min=23ms  med=156ms  max=987ms  p(95)=654ms  p(99)=890ms
http_req_failed................: 0.00%  ✓ 0   ✗ 127
```

**Checks:**
- ✓ = Passed
- ✗ = Failed (indicates API bug or breaking change)
- Percentage shows pass rate

**Metrics:**
- `http_req_duration`: Response time distribution
- `http_req_failed`: Error rate (should be ~0%)
- `http_reqs`: Total requests made

**Exit Codes:**
- `0` = All tests passed
- Non-zero = Failures occurred

## Continuous Integration

Tests run automatically in GitHub Actions:

### Functional Tests Workflow

**Triggers:**
- Every pull request to `main`
- Every push to `main`

**Steps:**
1. Spin up PostgreSQL service container
2. Initialize database schema
3. Load seed data
4. Build and start API
5. Run k6 functional tests
6. Upload results as artifacts (7 day retention)
7. Comment on PR with test summary

### Load Tests Workflow

**Triggers:**
- Push to `main` branch
- Nightly at 2 AM UTC (cron schedule)
- Manual trigger (workflow_dispatch)

**Steps:**
- Same as functional tests but runs load test suite
- Comments on commit with performance metrics

### Viewing Results

**GitHub Actions UI:**
1. Go to repository → Actions tab
2. Select workflow run
3. View "E2E Functional Test Results" or "E2E Load Test Results"
4. Download artifacts for detailed analysis

**Pull Request Comments:**
- Functional test summary appears automatically on PRs
- Shows pass/fail status for each test group
- Highlights failures with details

## Adding New Tests

### Add a New Functional Test

Edit `functional-tests.js` and add to the appropriate group:

```javascript
group('08 - New Feature', () => {
  const res = http.get(`${BASE_URL}/api/new-endpoint`);

  check(res, {
    'returns 200': (r) => r.status === 200,
    'has required field': (r) => {
      const body = JSON.parse(r.body);
      return body.data.requiredField !== undefined;
    },
    'response structure valid': (r) => {
      const body = JSON.parse(r.body);
      return body.success === true && body.data;
    }
  });
});
```

### Add a New Load Test Endpoint

Edit `load-tests.js` and add to the `endpoints` array:

```javascript
const endpoints = [
  // ... existing endpoints ...
  '/api/new-endpoint?param=value'
];
```

### Run Locally Before Committing

```bash
# Ensure API is running
curl http://localhost:5280/health

# Run your new tests
k6 run tests/e2e/functional-tests.js

# If all pass, commit and push
git add tests/e2e/functional-tests.js
git commit -m "test: add e2e tests for new feature"
git push
```

CI will run tests automatically on your PR.

## Troubleshooting

### API Not Responding

**Symptom:** Tests fail immediately with connection errors

**Check:**
```bash
curl http://localhost:5280/health
```

**Solution:**
- Ensure API is running: `dotnet run` in `src/HotWind.Api`
- Check API is listening on correct port (5280 by default)
- Verify firewall isn't blocking connections

### Database Connection Errors

**Symptom:** API starts but tests fail with 500 errors

**Check:**
```bash
psql -U hotwind_user -d hotwind -c "SELECT COUNT(*) FROM heater_models;"
```

**Solution:**
- Verify PostgreSQL is running
- Check connection string in `src/HotWind.Api/appsettings.json`
- Ensure database is initialized with `scripts/schema.sql` and `scripts/seed-data.sql`
- Grant proper permissions to hotwind_user

### Tests Fail on Invoice Creation

**Symptom:** Invoice creation tests return 400 errors

**Possible Causes:**
1. **Insufficient stock:** Seed data not loaded properly
   ```bash
   psql -U hotwind_user -d hotwind -c "SELECT SUM(quantity_remaining) FROM purchase_lots;"
   ```
   Should return > 0

2. **Invalid customer/SKU:** Test data assumptions changed
   - Check that seed data includes at least one customer
   - Verify heater models exist in database

**Solution:**
- Reload seed data: `psql -U hotwind_user -d hotwind -f scripts/seed-data.sql`
- Ensure FIFO logic hasn't regressed

### k6 Command Not Found

**Solution:**
- Install k6 (see Prerequisites above)
- Or use Docker:
  ```bash
  alias k6='docker run --rm --network host -v $(pwd):/work -w /work grafana/k6:1.3.0'
  ```

### Tests Pass Locally But Fail in CI

**Possible Causes:**
1. **Timing issues:** API not fully started before tests run
   - CI waits 30s for health check, might need adjustment
2. **Environment differences:** Different data in seed file
3. **Port conflicts:** Another service using 5280

**Debug in CI:**
1. Check workflow logs for startup errors
2. Download test results artifact
3. Compare `functional-summary.json` locally vs CI

## Performance Benchmarks

Expected response times (p95) from load tests:

| Endpoint Type | p95 Target | Notes |
|---------------|------------|-------|
| Health check | < 50ms | Simple endpoint, no DB |
| Simple GET (customers, models) | < 200ms | Single table query |
| Search queries | < 500ms | LIKE/ILIKE with indexes |
| Complex reports | < 1500ms | Multi-table joins, CTEs |
| Invoice creation | < 800ms | FIFO logic, multiple writes |

**Degradation Alerts:**
- If p95 increases by > 50% from baseline, investigate
- If error rate > 0.5%, investigate immediately
- If throughput drops below 10 req/sec, check resource constraints

## Advanced Usage

### Run Specific Test Groups

k6 doesn't support test selection out of the box, but you can comment out groups:

```javascript
export default function() {
  // group('01 - Health Check', () => { ... });  // Commented out
  group('02 - Models API', () => { ... });       // Only this runs
}
```

### Adjust Load Test Parameters

Edit `load-tests.js` options:

```javascript
export const options = {
  scenarios: {
    smoke_load: {
      executor: 'constant-vus',
      vus: 20,              // Increase VUs
      duration: '60s'       // Longer test
    }
  }
};
```

### Run Against Production

⚠️ **Warning:** Load tests will create data (invoices, exchange rates)

```bash
# Functional tests only (safer)
API_BASE_URL=https://api.production.example.com k6 run tests/e2e/functional-tests.js

# Load tests (use caution - will generate load)
API_BASE_URL=https://api.production.example.com k6 run tests/e2e/load-tests.js
```

## Best Practices

1. **Run functional tests before every commit**
   - Catches regressions early
   - Fast feedback loop (~30 seconds)

2. **Review test failures carefully**
   - API 400/404 errors are expected for negative tests
   - API 500 errors indicate bugs
   - Connection errors indicate environment issues

3. **Keep tests maintainable**
   - Use descriptive check names
   - Add comments for complex business logic tests
   - Update tests when API changes

4. **Monitor performance trends**
   - Compare load test results over time
   - Watch for gradual degradation
   - Set up alerts for threshold violations

5. **Update seed data carefully**
   - Tests assume specific data structure
   - Breaking changes to seed data require test updates
   - Document any assumptions in test comments

## Related Documentation

- [k6 Documentation](https://grafana.com/docs/k6/latest/)
- [API Documentation](../../src/HotWind.Api/README.md)
- [Database Schema](../../DATABASE.md)
- [CI/CD Setup](../../.github/SETUP_CICD.md)
