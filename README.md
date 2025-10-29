# Note
This is me just trying out Claude Code, an **AI Code Generator**. If something doesn't make sense, I probably agree with you.

# HotWind - Industrial Heating Equipment Retail System

A comprehensive educational demonstration project showcasing SQL query optimization techniques and modern .NET 9 features through a realistic B2B retail management system.

## Overview

HotWind is a three-layer application for managing industrial heating equipment inventory, purchases from international vendors, sales to B2B customers, and multi-currency financial reporting.

**Technology Stack:**
- **CLI**: .NET 9 Console Application with Spectre.Console for rich terminal UI
- **API**: ASP.NET Core 9 with RESTful endpoints
- **Database**: PostgreSQL 17 with advanced query optimization

**Key Features:**
- Multi-currency purchase and inventory tracking
- Daily exchange rate management with historical backfill
- B2B sales invoice processing with FIFO inventory deduction
- Comprehensive financial reports with currency translation
- ASCII-formatted reports for terminal display

## Architecture

Three-layer architecture with explicit SQL queries (no ORM) to demonstrate query optimization:

1. **CLI Layer** - Interactive console interface for data entry and reporting
2. **API Layer** - RESTful services with business logic and validation
3. **Database Layer** - PostgreSQL with normalized schema and optimized indexes

See [ADR.md](ADR.md) for detailed architecture decisions and [DATABASE.md](DATABASE.md) for complete schema documentation.

## Educational Focus

This project demonstrates:
- **SQL Optimization**: Complex joins, CTEs, window functions, and temporal queries
- **Query Performance**: Strategic indexing and EXPLAIN ANALYZE usage
- **Modern .NET**: C# 13 features, ASP.NET Core 9 improvements, native dependency injection
- **Proper Separation**: Clear boundaries between UI, business logic, and data access
- **Real-world Complexity**: Multi-currency calculations, inventory tracking, financial reporting

## Quick Start

**Prerequisites:**
- .NET 9 SDK ([Download](https://dotnet.microsoft.com/download/dotnet/9.0))
- PostgreSQL 17 ([Download](https://www.postgresql.org/download/))

**Setup (5 minutes):**

```bash
# 1. Create database
psql -U postgres -c "CREATE DATABASE hotwind;"
psql -U postgres -c "CREATE USER hotwind_user WITH PASSWORD 'hotwind_pass';"
psql -U postgres -c "GRANT ALL PRIVILEGES ON DATABASE hotwind TO hotwind_user;"

# 2. Initialize schema
psql -U hotwind_user -d hotwind -f scripts/schema.sql

# 3. Load seed data
psql -U hotwind_user -d hotwind -f scripts/seed-data.sql

# 4. Build solution
dotnet build

# 5. Run API (terminal 1)
cd src/HotWind.Api && dotnet run

# 6. Run CLI (terminal 2)
cd src/HotWind.Cli && dotnet run
```

**See [SETUP.md](SETUP.md) for detailed instructions and troubleshooting.**

## Project Structure

```
sql-optimizer/
├── README.md                    # This file
├── DATABASE.md                  # Complete database schema documentation
├── ADR.md                       # Architecture decision record
├── scripts/
│   ├── schema.sql              # Database schema creation
│   └── seed-data.sql           # Sample data for demonstration
├── src/
│   ├── HotWind.Api/            # ASP.NET Core 9 Web API
│   └── HotWind.Cli/            # .NET 9 Console Application
└── tests/
    ├── HotWind.Api.Tests/      # API unit and integration tests
    └── HotWind.Cli.Tests/      # CLI unit tests
```

## Reports

The system generates three ASCII-formatted reports:

1. **Stock Report**: Current inventory levels with weighted average costs and list prices (all in UAH)
2. **Price List Report**: Comparison of weighted lot values versus current market values
3. **Currency Translation Report**: Sales analysis showing impact of exchange rate fluctuations over time

All reports demonstrate complex SQL aggregations optimized for performance.

## Quick Reference

### CLI Commands
- **Create Invoice**: Interactive workflow with customer/product search
- **Stock Report**: Current inventory with profit margins
- **Price List Report**: Lot value vs market value comparison
- **Currency Translation Report**: Exchange rate impact analysis (requires date range)
- **Generate Exchange Rates**: Backfill missing rates using random walk

### API Endpoints
```
POST   /api/invoices                          # Create invoice (FIFO deduction)
GET    /api/reports/stock                     # Stock report
GET    /api/reports/price-list                # Price list report
GET    /api/reports/currency-translation      # Currency report (requires ?from=&to=)
POST   /api/exchangerates/generate            # Generate rates
GET    /api/models?search=bosch&inStockOnly=true
GET    /api/customers?search=kyiv
```

### Example SQL Query (Stock Report)
```sql
-- Demonstrates: CTEs, LATERAL joins, temporal queries, aggregations
WITH lot_details AS (
  SELECT pl.sku, pl.quantity_remaining, pl.unit_price_original,
         v.currency_code, pl.purchase_date
  FROM purchase_lots pl
  JOIN purchase_orders po ON pl.po_id = po.po_id
  JOIN vendors v ON po.vendor_id = v.vendor_id
  WHERE pl.quantity_remaining > 0
)
SELECT ld.sku, hm.model_name,
       SUM(ld.quantity_remaining) as stock_level,
       SUM(ld.quantity_remaining * ld.unit_price_original * er.exchange_rate) /
         SUM(ld.quantity_remaining) as weighted_avg_price_uah
FROM lot_details ld
JOIN heater_models hm ON ld.sku = hm.sku
LATERAL (
  SELECT exchange_rate FROM exchange_rates
  WHERE from_currency = ld.currency_code AND to_currency = 'UAH'
    AND rate_date <= CURRENT_DATE
  ORDER BY rate_date DESC LIMIT 1
) er
GROUP BY ld.sku, hm.model_name;
```

### Technology Stack
- **API**: ASP.NET Core 9, Npgsql 9.0, Swashbuckle (Swagger)
- **CLI**: .NET 9, Spectre.Console 0.49, HttpClient
- **Database**: PostgreSQL 17
- **Tests**: xUnit, Moq

## Docker Deployment

Pre-built Docker images are available on Docker Hub:

```bash
# Run API
docker run -p 8080:8080 \
  -e ConnectionStrings__DefaultConnection="Host=postgres;Database=hotwind;..." \
  number27/hotwind-api:latest

# Run CLI
docker run -it \
  -e ApiSettings__BaseUrl="http://api:8080" \
  number27/hotwind-cli:latest
```

See **[DOCKER.md](DOCKER.md)** for complete deployment guide including Kubernetes best practices.

## Documentation

- **[DATABASE.md](DATABASE.md)** - Complete schema documentation with ER diagram
- **[ADR.md](ADR.md)** - Architecture decisions and design patterns
- **[SETUP.md](SETUP.md)** - Detailed setup guide and troubleshooting
- **[DOCKER.md](DOCKER.md)** - Docker and Kubernetes deployment guide
- **[tests/e2e/README.md](tests/e2e/README.md)** - End-to-end test suite documentation
- **[CONTRIBUTING.md](CONTRIBUTING.md)** - Contribution guidelines and commit conventions

## Testing

### Unit Tests
Core business logic is covered by unit tests using xUnit and Moq:
```bash
dotnet test
```

### End-to-End Tests
Comprehensive e2e test suite using Grafana k6 validates API correctness and performance:
- **Functional tests:** All endpoints, business logic, error handling
- **Load tests:** Performance under concurrent load (10 VUs, 30s)

**Quick start:**
```bash
# Ensure API is running
cd src/HotWind.Api && dotnet run

# Run tests (in another terminal)
k6 run tests/e2e/functional-tests.js
```

See **[tests/e2e/README.md](tests/e2e/README.md)** for detailed documentation.

## CI/CD

This project uses GitHub Actions for automated:
- Build and unit tests on every push
- **E2E functional tests** on every PR and push to main
- **E2E load tests** on push to main + nightly schedule
- Semantic versioning based on conventional commits
- Multi-architecture Docker image builds (amd64, arm64)
- Automated releases to Docker Hub and GitHub

Commit message format determines version bumps:
- `feat:` → Minor version (1.0.0 → 1.1.0)
- `fix:` → Patch version (1.0.0 → 1.0.1)
- `feat!:` or `BREAKING CHANGE:` → Major version (1.0.0 → 2.0.0)
