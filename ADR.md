# Architecture Decision Record - HotWind System

## Status
Proposed - Pending approval

## Context
HotWind requires a modern, maintainable system for managing their heating equipment retail business with focus on SQL query optimization demonstration and showcasing .NET 9 ecosystem features. The system must support multi-currency operations, inventory management, sales tracking, and comprehensive reporting.

## Decision

### 1. Three-Layer Architecture

#### Layer 1: CLI Application (HotWind.Cli)
**Responsibility**: User interface and interaction
- Console-based interface for data entry and report viewing
- API client for communication with backend
- Input validation and user experience optimization

#### Layer 2: Web API (HotWind.Api)
**Responsibility**: Business logic and API contracts
- RESTful endpoints for all business operations
- Business rule enforcement
- Data validation and transformation
- Response formatting

#### Layer 3: PostgreSQL 17 Database
**Responsibility**: Data persistence and complex queries
- ACID compliance for transactions
- Complex aggregations and joins
- Exchange rate temporal queries
- Inventory tracking with constraints

### 2. Technology Stack

#### API: ASP.NET Core 9
**Rationale**:
- Latest framework with performance improvements
- Native OpenAPI/Swagger support for API documentation
- Keyed dependency injection for multiple repository implementations
- Enhanced minimal API features (though we'll use controllers for clarity)
- Built-in health checks and monitoring

**ASP.NET Core 9 Features to Showcase**:
1. **Keyed Services**: Different database connection strategies for read/write operations
2. **Enhanced Parameter Binding**: Complex DTO binding from query strings
3. **Improved OpenAPI Support**: Automatic API documentation with examples
4. **Native AOT Compatibility Patterns**: Even if not fully AOT compiled, follow patterns
5. **Request Delegate Generator**: For minimal API endpoints if used

#### CLI: .NET 9 Console Application
**Rationale**:
- Modern C# 13 features (file-scoped types, collection expressions)
- HttpClient for API communication with resilience patterns
- Rich console experience with Spectre.Console library for tables/prompts
- System.CommandLine for structured command parsing

#### Database: PostgreSQL 17
**Rationale**:
- Industry-leading open-source RDBMS
- Excellent query optimizer for teaching purposes
- EXPLAIN ANALYZE for query optimization demonstrations
- Advanced indexing strategies (B-tree, Hash, GiST)
- Common Table Expressions (CTEs) and window functions
- Native JSON support for flexible data structures

### 3. No ORM - Raw SQL with Parameters

**Decision**: Use ADO.NET with Npgsql directly, no Entity Framework

**Rationale**:
1. **Educational Value**: Students see actual SQL queries being executed
2. **Query Optimization**: Can directly optimize and observe SQL performance
3. **Transparency**: EXPLAIN plans directly correlate to written queries
4. **Performance**: Eliminates ORM overhead and N+1 query patterns
5. **Control**: Fine-grained control over transactions and connection management

**Implementation Pattern**:
```csharp
// Repository pattern with explicit SQL
public class HeaterModelRepository : IHeaterModelRepository
{
    private readonly NpgsqlDataSource _dataSource;

    public async Task<HeaterModel?> GetBySkuAsync(string sku)
    {
        await using var connection = await _dataSource.OpenConnectionAsync();
        await using var command = new NpgsqlCommand(
            "SELECT sku, model_name, manufacturer, capacity_kw, description " +
            "FROM heater_models WHERE sku = $1", connection);
        command.Parameters.AddWithValue(sku);
        // ... mapping logic
    }
}
```

### 4. Project Structure

#### HotWind.Api Project Structure
```
HotWind.Api/
├── Controllers/
│   ├── InvoicesController.cs       # Invoice management endpoints
│   ├── ReportsController.cs        # Report generation endpoints
│   ├── ExchangeRatesController.cs  # Exchange rate management
│   ├── ModelsController.cs         # Heater model lookup
│   └── CustomersController.cs      # Customer lookup
├── Services/
│   ├── IInvoiceService.cs
│   ├── InvoiceService.cs           # Business logic for invoices
│   ├── IReportService.cs
│   ├── ReportService.cs            # Report generation logic
│   ├── IExchangeRateService.cs
│   └── ExchangeRateService.cs      # Rate calculation and generation
├── Data/
│   ├── Repositories/
│   │   ├── IInvoiceRepository.cs
│   │   ├── InvoiceRepository.cs    # Data access for invoices
│   │   ├── IHeaterModelRepository.cs
│   │   ├── HeaterModelRepository.cs
│   │   ├── ICustomerRepository.cs
│   │   ├── CustomerRepository.cs
│   │   ├── IExchangeRateRepository.cs
│   │   ├── ExchangeRateRepository.cs
│   │   ├── IPurchaseLotRepository.cs
│   │   └── PurchaseLotRepository.cs
│   └── DatabaseConfig.cs           # Connection management
├── Models/
│   ├── Domain/                     # Database entities
│   │   ├── HeaterModel.cs
│   │   ├── Invoice.cs
│   │   ├── InvoiceLine.cs
│   │   ├── Customer.cs
│   │   ├── PurchaseLot.cs
│   │   ├── ExchangeRate.cs
│   │   └── Vendor.cs
│   ├── Dtos/                       # Data transfer objects
│   │   ├── CreateInvoiceDto.cs
│   │   ├── InvoiceDto.cs
│   │   ├── StockReportDto.cs
│   │   ├── PriceListReportDto.cs
│   │   └── CurrencyTranslationReportDto.cs
│   └── Requests/                   # API request models
│       ├── CreateInvoiceRequest.cs
│       ├── GenerateRatesRequest.cs
│       └── ReportParametersRequest.cs
├── Extensions/
│   └── ServiceCollectionExtensions.cs  # DI setup
├── Middleware/
│   ├── ExceptionHandlingMiddleware.cs
│   └── RequestLoggingMiddleware.cs
├── appsettings.json
├── appsettings.Development.json
├── Program.cs                      # Startup and configuration
└── HotWind.Api.csproj
```

#### HotWind.Cli Project Structure
```
HotWind.Cli/
├── Commands/
│   ├── ICommand.cs                 # Command interface
│   ├── CreateInvoiceCommand.cs     # Invoice entry workflow
│   ├── GenerateRatesCommand.cs     # Exchange rate generation
│   ├── StockReportCommand.cs       # Stock report display
│   ├── PriceListReportCommand.cs   # Price list report
│   └── CurrencyReportCommand.cs    # Currency translation report
├── Services/
│   ├── IApiClient.cs
│   ├── ApiClient.cs                # HTTP client wrapper
│   ├── IConsoleService.cs
│   └── ConsoleService.cs           # Console UI helpers
├── UI/
│   ├── TableRenderer.cs            # ASCII table rendering
│   ├── PromptHelper.cs             # Input prompts with suggestions
│   └── MenuBuilder.cs              # Menu construction
├── Models/                         # DTOs matching API
│   ├── InvoiceDto.cs
│   ├── HeaterModelDto.cs
│   ├── CustomerDto.cs
│   └── ReportDtos.cs
├── Configuration/
│   └── AppSettings.cs              # Configuration models
├── appsettings.json
├── Program.cs                      # Entry point and command routing
└── HotWind.Cli.csproj
```

### 5. API Design Patterns

#### RESTful Endpoints
```
POST   /api/invoices                    # Create new invoice
GET    /api/invoices/{id}               # Get invoice details
GET    /api/invoices                    # List invoices (paginated)

GET    /api/reports/stock               # Stock report
GET    /api/reports/price-list          # Price list report
GET    /api/reports/currency-translation?from=2024-01-01&to=2024-12-31

POST   /api/exchange-rates/generate     # Generate historical rates
GET    /api/exchange-rates/{from}/{to}?date=2024-01-01

GET    /api/models                      # List heater models (with search)
GET    /api/models/{sku}                # Get model details

GET    /api/customers                   # List customers (with search)
GET    /api/customers/{id}              # Get customer details
```

#### Response Format
```csharp
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Error { get; set; }
    public List<string>? ValidationErrors { get; set; }
}
```

### 6. Key Design Decisions

#### 6.1 Exchange Rate Lookup Strategy
**Decision**: Implement backward-looking rate resolution at query time

**SQL Pattern**:
```sql
-- Get exchange rate for specific date (or most recent before)
SELECT exchange_rate
FROM exchange_rates
WHERE from_currency = $1
  AND to_currency = 'UAH'
  AND rate_date <= $2
ORDER BY rate_date DESC
LIMIT 1
```

**Rationale**: Demonstrates SQL optimization with composite indexes and temporal queries

#### 6.2 Inventory Deduction Strategy
**Decision**: FIFO (First In, First Out) lot deduction

**Implementation**: When invoice is created, reduce `quantity_remaining` from oldest lots first

**Rationale**: Common inventory accounting method, demonstrates transaction handling

#### 6.3 Report Generation Strategy
**Decision**: Server-side report calculation with rich SQL queries

**Reports use**:
- Window functions for running totals
- CTEs for complex multi-step calculations
- Joins across multiple tables with proper indexing
- Weighted average calculations

**Example Stock Report Query**:
```sql
WITH lot_details AS (
  SELECT
    l.sku,
    l.quantity_remaining,
    l.unit_price_original,
    v.currency_code,
    l.purchase_date,
    er.exchange_rate
  FROM purchase_lots l
  JOIN purchase_orders po ON l.po_id = po.po_id
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
  WHERE l.quantity_remaining > 0
)
SELECT
  ld.sku,
  hm.model_name,
  SUM(ld.quantity_remaining) as stock_level,
  SUM(ld.quantity_remaining * ld.unit_price_original * ld.exchange_rate) /
    SUM(ld.quantity_remaining) as avg_purchase_price_uah,
  lp.list_price_uah
FROM lot_details ld
JOIN heater_models hm ON ld.sku = hm.sku
JOIN list_prices lp ON ld.sku = lp.sku AND lp.is_current = true
GROUP BY ld.sku, hm.model_name, lp.list_price_uah
ORDER BY ld.sku;
```

#### 6.4 Random Walk Exchange Rate Generation
**Decision**: Geometric Brownian Motion for rate generation

**Formula**: `rate(t+1) = rate(t) * exp((μ - σ²/2) * Δt + σ * √Δt * Z)`

Where:
- μ = drift (0.0001, slight upward bias)
- σ = volatility (0.015, 1.5% daily volatility)
- Z = standard normal random variable
- Δt = 1 day

**Rationale**: Produces realistic currency movements, demonstrates procedural data generation

#### 6.5 ASCII Table Rendering
**Decision**: Use Spectre.Console library for rich console tables

**Features**:
- Column alignment (left for text, right for numbers)
- Automatic width calculation
- Header emphasis
- Border styles

**Alternative**: Custom implementation if demonstrating algorithms

### 7. Error Handling Strategy

#### API Level
1. **Global exception middleware**: Catches unhandled exceptions
2. **Validation errors**: Return 400 with detailed validation messages
3. **Business rule violations**: Return 422 Unprocessable Entity
4. **Not found**: Return 404 with helpful message
5. **Database errors**: Log details, return generic 500 to client

#### CLI Level
1. **API communication errors**: Retry with exponential backoff
2. **User input validation**: Prompt again with error message
3. **Display errors**: Show formatted error messages from API

### 8. Configuration Management

#### API Configuration (appsettings.json)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=hotwind;Username=hotwind_user;Password=..."
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "CorsOrigins": ["http://localhost:5000"]
}
```

#### CLI Configuration (appsettings.json)
```json
{
  "ApiSettings": {
    "BaseUrl": "http://localhost:5280",
    "TimeoutSeconds": 30
  },
  "Display": {
    "PageSize": 20,
    "DateFormat": "yyyy-MM-dd"
  }
}
```

### 9. Testing Strategy

#### Unit Tests
- Service layer business logic
- Repository SQL query correctness (with test database)
- Exchange rate calculations
- Report aggregation logic

#### Integration Tests
- API endpoint contracts
- Database transactions
- End-to-end report generation

#### Manual Testing Focus
- CLI user experience
- Report readability
- Error message clarity

### 10. Database Connection Management

**Decision**: Use NpgsqlDataSource (connection pooling)

```csharp
// In Program.cs
var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
var dataSource = dataSourceBuilder.Build();
builder.Services.AddSingleton(dataSource);
```

**Rationale**:
- Connection pooling automatic
- Thread-safe
- Recommended by Npgsql documentation
- Better performance than managing connections manually

### 11. Transaction Management

**Decision**: Explicit transactions for multi-statement operations

**Pattern**:
```csharp
await using var connection = await _dataSource.OpenConnectionAsync();
await using var transaction = await connection.BeginTransactionAsync();
try
{
    // Multiple operations
    await transaction.CommitAsync();
}
catch
{
    await transaction.RollbackAsync();
    throw;
}
```

**Use Cases**:
- Creating invoice with multiple lines and updating lot quantities
- Batch exchange rate generation
- Purchase order with multiple lots

### 12. Security Considerations

**Current Scope**: Educational project, minimal security
- SQL injection prevention via parameterized queries
- Input validation on all endpoints
- Connection string in appsettings (not hardcoded)

**Production Enhancements** (out of scope):
- Authentication/Authorization (JWT tokens)
- API rate limiting
- Encrypted connections (SSL)
- Secrets management (Azure Key Vault, HashiCorp Vault)

## Consequences

### Positive
1. **Clear separation of concerns** between layers
2. **Direct SQL visibility** for optimization teaching
3. **Modern .NET features** demonstrated throughout
4. **Realistic business domain** with complexity
5. **Excellent query optimization opportunities**

### Negative
1. **No ORM conveniences** - more boilerplate code
2. **Manual SQL to object mapping** required
3. **Schema changes** require updates in multiple places
4. **More verbose** than EF Core implementations

### Neutral
1. **Learning curve** for students on raw SQL patterns
2. **Trade-off between abstraction and control** clearly visible

## Alternatives Considered

### Entity Framework Core
**Rejected**: While modern and productive, it obscures the SQL queries that are the focus of this educational project. EF's query translation can be unpredictable for optimization teaching.

### Dapper
**Considered**: Micro-ORM that would reduce boilerplate while maintaining SQL visibility. Rejected to demonstrate pure ADO.NET patterns and give maximum control.

### Minimal APIs
**Partial Use**: Will use primarily controllers for better organization, but may demonstrate minimal API patterns for simple endpoints.

### Microservices Architecture
**Rejected**: Overkill for scope. Three-layer monolith is more appropriate for educational context and easier to deploy/demonstrate.

## References
- ASP.NET Core 9 Documentation
- PostgreSQL 17 Documentation
- Npgsql Documentation
- C# 13 Language Reference
- Three-Layer Architecture Pattern
