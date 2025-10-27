# Database Schema for HotWind Heating Equipment System

## Overview
This database schema supports HotWind's heating equipment retail business, tracking inventory purchases from international vendors, sales to customers, and multi-currency exchange rates for financial reporting.

## Schema Design Principles
- **Third Normal Form (3NF)** compliance for data integrity
- **Composite keys** for temporal data (exchange rates)
- **Audit columns** for tracking data changes
- **Check constraints** for business rule enforcement
- **Indexes** optimized for reporting queries

## Entity Relationship Diagram

```
┌─────────────────┐
│   Countries     │
├─────────────────┤
│ country_code PK │
│ country_name    │
└────────┬────────┘
         │
         │ 1
         │
         │ N
┌────────┴────────────┐         ┌──────────────────┐
│      Vendors        │    N    │   Currencies     │
├─────────────────────┤────────>├──────────────────┤
│ vendor_id       PK  │         │ currency_code PK │
│ vendor_name         │         │ currency_name    │
│ country_code    FK  │         │ symbol           │
│ currency_code   FK  │         └────────┬─────────┘
│ contact_info        │                  │
└────────┬────────────┘                  │
         │                               │
         │ 1                             │
         │                               │
         │ N                             │
┌────────┴─────────────┐          ┌──────┴───────────────┐
│  Purchase Orders     │          │  Exchange Rates      │
├──────────────────────┤          ├──────────────────────┤
│ po_id            PK  │          │ from_currency    PK,FK│
│ vendor_id        FK  │          │ to_currency      PK,FK│
│ po_date              │          │ rate_date        PK  │
│ total_amount         │          │ exchange_rate        │
│ notes                │          │ created_at           │
└────────┬─────────────┘          └──────────────────────┘
         │
         │ 1
         │
         │ N                      ┌──────────────────┐
┌────────┴─────────────┐    N    │  Heater Models   │
│  Purchase Lots       │────────>├──────────────────┤
├──────────────────────┤         │ sku          PK  │
│ lot_id           PK  │         │ model_name       │
│ po_id            FK  │         │ manufacturer     │
│ sku              FK  │         │ capacity_kw      │
│ lot_number           │         │ description      │
│ quantity_purchased   │         └────────┬─────────┘
│ quantity_remaining   │                  │
│ unit_price_original  │                  │
│ purchase_date        │                  │
└──────────────────────┘                  │
                                          │
                                          │ N
         ┌────────────────┐               │
         │   Customers    │               │
         ├────────────────┤               │
         │ customer_id PK │               │
         │ company_name   │               │
         │ contact_person │               │
         │ email          │               │
         │ phone          │               │
         └────────┬───────┘               │
                  │                       │
                  │ 1                     │
                  │                       │
                  │ N                     │
         ┌────────┴────────┐              │
         │    Invoices     │              │
         ├─────────────────┤              │
         │ invoice_id  PK  │              │
         │ customer_id FK  │              │
         │ invoice_date    │              │
         │ total_amount    │              │
         │ notes           │              │
         └────────┬────────┘              │
                  │                       │
                  │ 1                     │
                  │                       │
                  │ N                     │
         ┌────────┴─────────┐       ┌─────┴────────────┐
         │  Invoice Lines   │   N   │  List Prices     │
         ├──────────────────┤──────>├──────────────────┤
         │ invoice_line_id PK│       │ price_id     PK  │
         │ invoice_id     FK │       │ sku          FK  │
         │ sku            FK │       │ list_price_uah   │
         │ quantity_sold     │       │ effective_date   │
         │ unit_price_uah    │       │ is_current       │
         └───────────────────┘       └──────────────────┘
```

## Table Definitions

### Core Reference Data

#### countries
Lookup table for country information.
```sql
CREATE TABLE countries (
    country_code    CHAR(2) PRIMARY KEY,
    country_name    VARCHAR(100) NOT NULL UNIQUE
);
```

#### currencies
ISO currency codes and metadata.
```sql
CREATE TABLE currencies (
    currency_code   CHAR(3) PRIMARY KEY,
    currency_name   VARCHAR(100) NOT NULL,
    symbol          VARCHAR(10)
);
```

#### exchange_rates
Daily exchange rates for all currency pairs to UAH. Uses composite primary key for temporal data.
```sql
CREATE TABLE exchange_rates (
    from_currency   CHAR(3) NOT NULL REFERENCES currencies(currency_code),
    to_currency     CHAR(3) NOT NULL REFERENCES currencies(currency_code),
    rate_date       DATE NOT NULL,
    exchange_rate   DECIMAL(15, 6) NOT NULL CHECK (exchange_rate > 0),
    created_at      TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (from_currency, to_currency, rate_date),
    CONSTRAINT chk_different_currencies CHECK (from_currency <> to_currency)
);

CREATE INDEX idx_exchange_rates_lookup ON exchange_rates(from_currency, to_currency, rate_date DESC);
```

### Product Catalog

#### heater_models
Catalog of all heater models tracked in the system.
```sql
CREATE TABLE heater_models (
    sku             VARCHAR(50) PRIMARY KEY,
    model_name      VARCHAR(200) NOT NULL,
    manufacturer    VARCHAR(100) NOT NULL,
    capacity_kw     DECIMAL(8, 2),
    description     TEXT,
    created_at      TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_heater_models_name ON heater_models(model_name);
```

#### list_prices
Current and historical list prices for heater models in UAH.
```sql
CREATE TABLE list_prices (
    price_id        SERIAL PRIMARY KEY,
    sku             VARCHAR(50) NOT NULL REFERENCES heater_models(sku),
    list_price_uah  DECIMAL(15, 2) NOT NULL CHECK (list_price_uah >= 0),
    effective_date  DATE NOT NULL,
    is_current      BOOLEAN NOT NULL DEFAULT true,
    created_at      TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UNIQUE (sku, effective_date)
);

CREATE INDEX idx_list_prices_current ON list_prices(sku, is_current) WHERE is_current = true;
CREATE INDEX idx_list_prices_effective ON list_prices(sku, effective_date DESC);
```

### Vendors and Purchases

#### vendors
International suppliers with their associated currency and country.
```sql
CREATE TABLE vendors (
    vendor_id       SERIAL PRIMARY KEY,
    vendor_name     VARCHAR(200) NOT NULL,
    country_code    CHAR(2) NOT NULL REFERENCES countries(country_code),
    currency_code   CHAR(3) NOT NULL REFERENCES currencies(currency_code),
    contact_info    TEXT,
    created_at      TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_vendors_country ON vendors(country_code);
```

#### purchase_orders
Purchase orders from vendors.
```sql
CREATE TABLE purchase_orders (
    po_id           SERIAL PRIMARY KEY,
    vendor_id       INTEGER NOT NULL REFERENCES vendors(vendor_id),
    po_date         DATE NOT NULL,
    total_amount    DECIMAL(15, 2),
    notes           TEXT,
    created_at      TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_purchase_orders_date ON purchase_orders(po_date DESC);
CREATE INDEX idx_purchase_orders_vendor ON purchase_orders(vendor_id);
```

#### purchase_lots
Individual lots from purchase orders. Tracks inventory levels.
```sql
CREATE TABLE purchase_lots (
    lot_id              SERIAL PRIMARY KEY,
    po_id               INTEGER NOT NULL REFERENCES purchase_orders(po_id),
    sku                 VARCHAR(50) NOT NULL REFERENCES heater_models(sku),
    lot_number          VARCHAR(100) NOT NULL,
    quantity_purchased  INTEGER NOT NULL CHECK (quantity_purchased > 0),
    quantity_remaining  INTEGER NOT NULL CHECK (quantity_remaining >= 0),
    unit_price_original DECIMAL(15, 2) NOT NULL CHECK (unit_price_original >= 0),
    purchase_date       DATE NOT NULL,
    created_at          TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT chk_quantity_valid CHECK (quantity_remaining <= quantity_purchased),
    UNIQUE (lot_number)
);

CREATE INDEX idx_purchase_lots_sku ON purchase_lots(sku);
CREATE INDEX idx_purchase_lots_po ON purchase_lots(po_id);
CREATE INDEX idx_purchase_lots_remaining ON purchase_lots(sku, quantity_remaining) WHERE quantity_remaining > 0;
```

### Customers and Sales

#### customers
B2B customers with minimal information.
```sql
CREATE TABLE customers (
    customer_id     SERIAL PRIMARY KEY,
    company_name    VARCHAR(200) NOT NULL,
    contact_person  VARCHAR(100),
    email           VARCHAR(100),
    phone           VARCHAR(50),
    created_at      TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_customers_company ON customers(company_name);
```

#### invoices
Sales invoices to customers (always in UAH).
```sql
CREATE TABLE invoices (
    invoice_id      SERIAL PRIMARY KEY,
    customer_id     INTEGER NOT NULL REFERENCES customers(customer_id),
    invoice_date    DATE NOT NULL,
    total_amount    DECIMAL(15, 2) NOT NULL CHECK (total_amount >= 0),
    notes           TEXT,
    created_at      TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_invoices_date ON invoices(invoice_date DESC);
CREATE INDEX idx_invoices_customer ON invoices(customer_id);
```

#### invoice_lines
Line items for invoices.
```sql
CREATE TABLE invoice_lines (
    invoice_line_id SERIAL PRIMARY KEY,
    invoice_id      INTEGER NOT NULL REFERENCES invoices(invoice_id),
    sku             VARCHAR(50) NOT NULL REFERENCES heater_models(sku),
    quantity_sold   INTEGER NOT NULL CHECK (quantity_sold > 0),
    unit_price_uah  DECIMAL(15, 2) NOT NULL CHECK (unit_price_uah >= 0),
    created_at      TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_invoice_lines_invoice ON invoice_lines(invoice_id);
CREATE INDEX idx_invoice_lines_sku ON invoice_lines(sku);
CREATE INDEX idx_invoice_lines_date_sku ON invoice_lines(invoice_id, sku);
```

## Views for Reporting

### v_current_stock
Aggregates inventory levels by SKU with weighted average purchase costs.
```sql
CREATE VIEW v_current_stock AS
SELECT
    l.sku,
    h.model_name,
    h.manufacturer,
    SUM(l.quantity_remaining) as total_in_stock,
    COUNT(DISTINCT l.lot_id) as lot_count,
    SUM(l.quantity_remaining * l.unit_price_original) / NULLIF(SUM(l.quantity_remaining), 0) as weighted_avg_cost_original
FROM purchase_lots l
JOIN heater_models h ON l.sku = h.sku
WHERE l.quantity_remaining > 0
GROUP BY l.sku, h.model_name, h.manufacturer;
```

### v_current_exchange_rates
Latest exchange rate for each currency pair.
```sql
CREATE VIEW v_current_exchange_rates AS
SELECT DISTINCT ON (from_currency, to_currency)
    from_currency,
    to_currency,
    rate_date,
    exchange_rate
FROM exchange_rates
ORDER BY from_currency, to_currency, rate_date DESC;
```

### v_current_list_prices
Current list prices for all models.
```sql
CREATE VIEW v_current_list_prices AS
SELECT
    sku,
    list_price_uah,
    effective_date
FROM list_prices
WHERE is_current = true;
```

## Key Indexes for Query Optimization

The schema includes strategic indexes for common query patterns:

1. **Temporal lookups**: Exchange rates by date (DESC order for latest-first queries)
2. **Inventory queries**: SKU + remaining quantity for stock reports
3. **Sales analysis**: Invoice date and SKU for period-based reports
4. **Reference data**: Foreign key indexes for join optimization

## Business Logic Enforcement

1. **Exchange rates**: Must be positive, currencies must differ
2. **Quantities**: Purchased > 0, remaining >= 0, remaining <= purchased
3. **Prices**: Non-negative constraints on all price fields
4. **Temporal data**: Unique constraints on date-dependent data

## Sample Data Requirements

For demonstration purposes, the database should include:
- Multiple vendors from different countries (USA, Germany, China, Poland)
- Currencies: UAH, USD, EUR, CNY, PLN
- 20-30 heater models from various manufacturers
- Historical purchase orders spanning 6-12 months
- Daily exchange rates for the same period
- Sample invoices demonstrating sales patterns
- Current list prices for all models in stock

## Normalization Analysis

### 1NF (First Normal Form)
- All tables have atomic values
- Each column contains single values
- Primary keys defined for all tables

### 2NF (Second Normal Form)
- No partial dependencies on composite keys
- Exchange rates properly keyed on (from_currency, to_currency, rate_date)

### 3NF (Third Normal Form)
- No transitive dependencies
- Country and Currency extracted as separate entities
- Vendor references both rather than storing denormalized data
- List prices separated from heater models for temporal tracking

## PostgreSQL 17 Specific Features

This schema can leverage PostgreSQL 17 capabilities:
- **JSONB columns** for flexible vendor/customer metadata (future enhancement)
- **Partitioning** on exchange_rates and invoices by date range
- **Generated columns** for computed values (future enhancement)
- **Row-level security** for multi-tenant scenarios (future enhancement)
