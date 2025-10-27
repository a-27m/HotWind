# HotWind Setup Guide

This guide will help you set up the HotWind system for educational demonstration of SQL query optimization.

## Prerequisites

- **.NET 9 SDK**: [Download](https://dotnet.microsoft.com/download/dotnet/9.0)
- **PostgreSQL 17**: [Download](https://www.postgresql.org/download/)
- **Git**: For version control
- **Terminal**: macOS Terminal, Windows PowerShell, or Linux shell

## Quick Start

### 1. Database Setup

Create the database and user:

```bash
# Connect to PostgreSQL as superuser
psql -U postgres

# Create database and user
CREATE DATABASE hotwind;
CREATE USER hotwind_user WITH PASSWORD 'hotwind_pass';
GRANT ALL PRIVILEGES ON DATABASE hotwind TO hotwind_user;

# Connect to hotwind database
\c hotwind

# Grant schema privileges
GRANT ALL ON SCHEMA public TO hotwind_user;
GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA public TO hotwind_user;
GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA public TO hotwind_user;
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT ALL ON TABLES TO hotwind_user;
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT ALL ON SEQUENCES TO hotwind_user;

\q
```

### 2. Create Schema

```bash
cd sql-optimizer
psql -U hotwind_user -d hotwind -f scripts/schema.sql
```

### 3. Generate and Load Seed Data

```bash
cd scripts
python3 generate_seed_data.py > seed-data.sql
psql -U hotwind_user -d hotwind -f seed-data.sql
```

This creates:
- ~366 days of exchange rates (4 currency pairs)
- 27 heater models
- 80 purchase orders with ~197 lots
- 15 customers
- 300 sales invoices with ~598 line items

### 4. Build and Run API

```bash
cd ../src/HotWind.Api
dotnet restore
dotnet build
dotnet run
```

The API will start on http://localhost:5280

You can browse the API documentation at: http://localhost:5280/swagger

### 5. Run CLI Application

In a separate terminal:

```bash
cd src/HotWind.Cli
dotnet restore
dotnet build
dotnet run
```

## Configuration

### API Configuration

Edit `src/HotWind.Api/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=hotwind;Username=hotwind_user;Password=hotwind_pass"
  }
}
```

### CLI Configuration

Edit `src/HotWind.Cli/appsettings.json`:

```json
{
  "ApiSettings": {
    "BaseUrl": "http://localhost:5280"
  }
}
```

## Running Tests

```bash
cd tests/HotWind.Api.Tests
dotnet test
```

## Using the System

### CLI Menu Options

1. **Create Invoice**
   - Interactive workflow for creating sales invoices
   - Searches for customers and products
   - Validates stock levels
   - Automatically deducts inventory using FIFO

2. **Stock Report**
   - Shows current inventory levels
   - Displays weighted average purchase prices
   - Calculates potential profit margins

3. **Price List Report**
   - Compares lot values to current market values
   - Shows value appreciation/depreciation

4. **Currency Translation Report**
   - Analyzes exchange rate impact on sales
   - Requires date range input
   - Demonstrates temporal query optimization

5. **Generate Exchange Rates**
   - Backfills missing exchange rates
   - Uses Geometric Brownian Motion
   - Idempotent (won't duplicate existing rates)

### API Endpoints

**Reports:**
- `GET /api/reports/stock`
- `GET /api/reports/price-list`
- `GET /api/reports/currency-translation?from=2024-01-01&to=2024-12-31`

**Invoices:**
- `POST /api/invoices`
- `GET /api/invoices/{id}`
- `GET /api/invoices`

**Exchange Rates:**
- `POST /api/exchangerates/generate`
- `GET /api/exchangerates/{from}/{to}?date=2024-01-15`

**Lookups:**
- `GET /api/models?search=bosch&inStockOnly=true`
- `GET /api/customers?search=kyiv`

## SQL Query Optimization Examples

### Viewing Query Plans

Connect to the database:

```bash
psql -U hotwind_user -d hotwind
```

Analyze a complex report query:

```sql
EXPLAIN ANALYZE
SELECT
    lv.sku,
    hm.model_name,
    SUM(lv.quantity_remaining) as stock_level
FROM (
    SELECT
        pl.sku,
        pl.quantity_remaining,
        (pl.unit_price_original * er.exchange_rate) as unit_price_uah
    FROM purchase_lots pl
    JOIN purchase_orders po ON pl.po_id = po.po_id
    JOIN vendors v ON po.vendor_id = v.vendor_id
    LATERAL (
        SELECT exchange_rate
        FROM exchange_rates
        WHERE from_currency = v.currency_code
          AND to_currency = 'UAH'
          AND rate_date <= CURRENT_DATE
        ORDER BY rate_date DESC
        LIMIT 1
    ) er
    WHERE pl.quantity_remaining > 0
) lv
JOIN heater_models hm ON lv.sku = hm.sku
GROUP BY lv.sku, hm.model_name;
```

### Key Optimization Scenarios

1. **Temporal Queries**: Exchange rate lookups with backward-looking resolution
2. **LATERAL Joins**: Correlated subqueries for rate lookups per row
3. **Partial Indexes**: `WHERE quantity_remaining > 0` index usage
4. **Aggregation Performance**: Weighted averages across large datasets
5. **Cardinality Changes**: Query plan changes as inventory depletes

### Monitoring Database Size

```sql
SELECT
    pg_database.datname,
    pg_size_pretty(pg_database_size(pg_database.datname)) AS size
FROM pg_database
WHERE datname = 'hotwind';
```

## Troubleshooting

### Connection Refused

Ensure PostgreSQL is running:

```bash
# macOS
brew services list

# Linux
sudo systemctl status postgresql

# Windows
# Check Services app for PostgreSQL
```

### Permission Denied

Re-run the GRANT commands from step 1.

### API Not Responding

Check the port is available:

```bash
lsof -i :5280
```

### CLI Cannot Connect to API

1. Verify API is running: http://localhost:5280/health
2. Check `appsettings.json` in CLI has correct URL
3. Check firewall settings

## Database Maintenance

### Reset Database

```bash
psql -U hotwind_user -d hotwind -c "DROP SCHEMA public CASCADE; CREATE SCHEMA public;"
psql -U hotwind_user -d hotwind -f scripts/schema.sql
psql -U hotwind_user -d hotwind -f scripts/seed-data.sql
```

### Backup Database

```bash
pg_dump -U hotwind_user hotwind > hotwind_backup.sql
```

### Restore Database

```bash
psql -U hotwind_user hotwind < hotwind_backup.sql
```

## Educational Features

This project demonstrates:

1. **Raw SQL vs ORM**: Direct SQL with Npgsql for transparency
2. **Repository Pattern**: Testable data access layer
3. **Service Layer**: Business logic separation
4. **Complex Queries**: CTEs, window functions, lateral joins
5. **Transaction Management**: ACID compliance for multi-step operations
6. **Parameterized Queries**: SQL injection prevention
7. **Index Strategy**: Covering, partial, and composite indexes
8. **Query Optimization**: EXPLAIN ANALYZE usage
9. **Cardinality Estimation**: Statistics and plan changes
10. **C# 13 Features**: File-scoped types, collection expressions

## Next Steps

1. Run the Stock Report to see current inventory
2. Create an invoice to observe FIFO inventory deduction
3. Generate exchange rates for missing date ranges
4. Run EXPLAIN ANALYZE on report queries
5. Observe query plan changes after creating more invoices
