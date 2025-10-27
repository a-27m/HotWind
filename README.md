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
- .NET 9 SDK
- PostgreSQL 17
- IDE (Visual Studio, Rider, or VS Code)

**Setup:**
```bash
# Clone and navigate to project
cd sql-optimizer

# Setup database (instructions in DATABASE.md)
psql -U postgres -f scripts/schema.sql
psql -U postgres hotwind -f scripts/seed-data.sql

# Configure connection strings in appsettings.json

# Run API
cd src/HotWind.Api
dotnet run

# Run CLI (in separate terminal)
cd src/HotWind.Cli
dotnet run
```

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

## License

Educational use only.
