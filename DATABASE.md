# Database Schema for HotWind Heating Equipment System

## Overview
This database schema supports HotWind's heating equipment retail business, tracking inventory purchases from international vendors, sales to customers, and multi-currency exchange rates for financial reporting.

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
│ po_id            PK  │          │ from_currency  PK,FK │
│ vendor_id        FK  │          │ to_currency    PK,FK │
│ po_date              │          │ rate_date      PK    │
│ total_amount         │          │ exchange_rate        │
│ notes                │          │ created_at           │
└────────┬─────────────┘          └──────────────────────┘
         │
         │ 1
         │
         │ N                     ┌──────────────────┐
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
         ┌────────┴──────────┐       ┌────┴─────────────┐
         │  Invoice Lines    │   N   │  List Prices     │
         ├───────────────────┤──────>├──────────────────┤
         │ invoice_line_id PK│       │ price_id     PK  │
         │ invoice_id     FK │       │ sku          FK  │
         │ sku            FK │       │ list_price_uah   │
         │ quantity_sold     │       │ effective_date   │
         │ unit_price_uah    │       │ is_current       │
         └───────────────────┘       └──────────────────┘
```

## Table Definitions

See `./scripts/schema.sql` for details

### Core Reference Data

#### countries
Lookup table for country information.

#### currencies
ISO currency codes and metadata.

#### exchange_rates
Daily exchange rates for all currency pairs to UAH. Uses composite primary key for temporal data.

### Product Catalog

#### heater_models
Catalog of all heater models tracked in the system.

#### list_prices
Current and historical list prices for heater models in UAH.

### Vendors and Purchases

#### vendors
International suppliers with their associated currency and country.

#### purchase_orders
Purchase orders from vendors.

#### purchase_lots
Individual lots from purchase orders. Tracks inventory levels.

### Customers and Sales

#### customers
B2B customers.

#### invoices
Sales invoices to customers (always in UAH).

#### invoice_lines
Line items for invoices.

## Views for Reporting

### v_current_stock
Aggregates inventory levels by SKU with weighted average purchase costs.

```sql
    ...
    SUM(l.quantity_remaining) as total_in_stock,
    COUNT(DISTINCT l.lot_id) as lot_count,
    SUM(l.quantity_remaining * l.unit_price_original) / NULLIF(SUM(l.quantity_remaining), 0) as weighted_avg_cost_original
    ...
```    

### v_current_exchange_rates
Latest exchange rate for each currency pair.

### v_current_list_prices
Current list prices for all models.

## Indexes

The schema includes indexes for common query patterns:

1. **Temporal lookups**: Exchange rates by date (DESC order for latest-first queries)
2. **Inventory queries**: SKU + remaining quantity for stock reports
3. **Sales analysis**: Invoice date and SKU for period-based reports
4. **Reference data**: Foreign key indexes for join optimization

## Business Logic Enforcement

1. **Exchange rates**: Must be positive, currencies must differ
2. **Quantities**: Purchased > 0, remaining >= 0, remaining <= purchased
3. **Prices**: Non-negative constraints on all price fields
4. **Temporal data**: Unique constraints on date-dependent data

## Sample Data Goals

For demonstration purposes, the database should include:
- Multiple vendors from different countries (USA, Germany, China, Poland)
- Currencies: UAH, USD, EUR, CNY, PLN
- 20-30 heater models from various manufacturers
- Historical purchase orders spanning 6-12 months
- Daily exchange rates for the same period
- Sample invoices demonstrating sales patterns
- Current list prices for all models in stock

## PostgreSQL 17 Specific Features

This schema can leverage PostgreSQL 17 capabilities:
- **JSONB columns** for flexible vendor/customer metadata (future enhancement)
- **Partitioning** on exchange_rates and invoices by date range
- **Generated columns** for computed values (future enhancement)
- **Row-level security** for multi-tenant scenarios (future enhancement)
