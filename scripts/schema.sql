-- HotWind Heating Equipment Retail System
-- PostgreSQL 17 Database Schema
-- Educational project for SQL query optimization

-- Drop existing objects if they exist (for clean reinstall)
DROP VIEW IF EXISTS v_current_list_prices CASCADE;
DROP VIEW IF EXISTS v_current_exchange_rates CASCADE;
DROP VIEW IF EXISTS v_current_stock CASCADE;

DROP TABLE IF EXISTS invoice_lines CASCADE;
DROP TABLE IF EXISTS invoices CASCADE;
DROP TABLE IF EXISTS customers CASCADE;
DROP TABLE IF EXISTS list_prices CASCADE;
DROP TABLE IF EXISTS purchase_lots CASCADE;
DROP TABLE IF EXISTS purchase_orders CASCADE;
DROP TABLE IF EXISTS vendors CASCADE;
DROP TABLE IF EXISTS heater_models CASCADE;
DROP TABLE IF EXISTS exchange_rates CASCADE;
DROP TABLE IF EXISTS currencies CASCADE;
DROP TABLE IF EXISTS countries CASCADE;

-- ============================================================================
-- REFERENCE DATA TABLES
-- ============================================================================

-- Countries lookup table
CREATE TABLE countries (
    country_code    CHAR(2) PRIMARY KEY,
    country_name    VARCHAR(100) NOT NULL UNIQUE,
    CONSTRAINT chk_country_code_format CHECK (country_code = UPPER(country_code))
);

COMMENT ON TABLE countries IS 'ISO 3166-1 alpha-2 country codes';

-- Currencies lookup table
CREATE TABLE currencies (
    currency_code   CHAR(3) PRIMARY KEY,
    currency_name   VARCHAR(100) NOT NULL,
    symbol          VARCHAR(10),
    CONSTRAINT chk_currency_code_format CHECK (currency_code = UPPER(currency_code))
);

COMMENT ON TABLE currencies IS 'ISO 4217 currency codes and metadata';

-- Exchange rates - temporal data with composite key
CREATE TABLE exchange_rates (
    from_currency   CHAR(3) NOT NULL REFERENCES currencies(currency_code),
    to_currency     CHAR(3) NOT NULL REFERENCES currencies(currency_code),
    rate_date       DATE NOT NULL,
    exchange_rate   DECIMAL(15, 6) NOT NULL CHECK (exchange_rate > 0),
    created_at      TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (from_currency, to_currency, rate_date),
    CONSTRAINT chk_different_currencies CHECK (from_currency <> to_currency)
);

COMMENT ON TABLE exchange_rates IS 'Daily exchange rates for currency pairs to UAH';
COMMENT ON COLUMN exchange_rates.exchange_rate IS 'How many to_currency units per 1 from_currency unit';

-- Index for efficient temporal lookups (most recent rate before a given date)
CREATE INDEX idx_exchange_rates_lookup ON exchange_rates(from_currency, to_currency, rate_date DESC);

-- ============================================================================
-- PRODUCT CATALOG
-- ============================================================================

-- Heater models catalog
CREATE TABLE heater_models (
    sku             VARCHAR(50) PRIMARY KEY,
    model_name      VARCHAR(200) NOT NULL,
    manufacturer    VARCHAR(100) NOT NULL,
    capacity_kw     DECIMAL(8, 2),
    description     TEXT,
    created_at      TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT chk_capacity_positive CHECK (capacity_kw IS NULL OR capacity_kw > 0)
);

COMMENT ON TABLE heater_models IS 'Catalog of heating equipment models';
COMMENT ON COLUMN heater_models.sku IS 'Stock Keeping Unit - unique product identifier';

CREATE INDEX idx_heater_models_name ON heater_models(model_name);
CREATE INDEX idx_heater_models_manufacturer ON heater_models(manufacturer);

-- List prices - current and historical pricing in UAH
CREATE TABLE list_prices (
    price_id        SERIAL PRIMARY KEY,
    sku             VARCHAR(50) NOT NULL REFERENCES heater_models(sku) ON DELETE CASCADE,
    list_price_uah  DECIMAL(15, 2) NOT NULL CHECK (list_price_uah >= 0),
    effective_date  DATE NOT NULL,
    is_current      BOOLEAN NOT NULL DEFAULT true,
    created_at      TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UNIQUE (sku, effective_date)
);

COMMENT ON TABLE list_prices IS 'Current and historical list prices for products in UAH';
COMMENT ON COLUMN list_prices.is_current IS 'Flag indicating the currently active price';

-- Only one current price per SKU
CREATE UNIQUE INDEX idx_list_prices_current_unique ON list_prices(sku) WHERE is_current = true;
CREATE INDEX idx_list_prices_effective ON list_prices(sku, effective_date DESC);

-- ============================================================================
-- VENDORS AND PURCHASES
-- ============================================================================

-- Vendors (suppliers)
CREATE TABLE vendors (
    vendor_id       SERIAL PRIMARY KEY,
    vendor_name     VARCHAR(200) NOT NULL,
    country_code    CHAR(2) NOT NULL REFERENCES countries(country_code),
    currency_code   CHAR(3) NOT NULL REFERENCES currencies(currency_code),
    contact_info    TEXT,
    created_at      TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

COMMENT ON TABLE vendors IS 'International suppliers with their associated currency';

CREATE INDEX idx_vendors_country ON vendors(country_code);
CREATE INDEX idx_vendors_name ON vendors(vendor_name);

-- Purchase orders from vendors
CREATE TABLE purchase_orders (
    po_id           SERIAL PRIMARY KEY,
    vendor_id       INTEGER NOT NULL REFERENCES vendors(vendor_id),
    po_date         DATE NOT NULL,
    total_amount    DECIMAL(15, 2),
    notes           TEXT,
    created_at      TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT chk_po_total_positive CHECK (total_amount IS NULL OR total_amount >= 0)
);

COMMENT ON TABLE purchase_orders IS 'Purchase orders from vendors';

CREATE INDEX idx_purchase_orders_date ON purchase_orders(po_date DESC);
CREATE INDEX idx_purchase_orders_vendor ON purchase_orders(vendor_id);

-- Purchase lots - individual line items from purchase orders
CREATE TABLE purchase_lots (
    lot_id              SERIAL PRIMARY KEY,
    po_id               INTEGER NOT NULL REFERENCES purchase_orders(po_id) ON DELETE CASCADE,
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

COMMENT ON TABLE purchase_lots IS 'Individual lots from purchase orders tracking inventory levels';
COMMENT ON COLUMN purchase_lots.quantity_remaining IS 'Current stock level for this lot (decremented on sales)';
COMMENT ON COLUMN purchase_lots.unit_price_original IS 'Purchase price per unit in vendor currency';

CREATE INDEX idx_purchase_lots_sku ON purchase_lots(sku);
CREATE INDEX idx_purchase_lots_po ON purchase_lots(po_id);
CREATE INDEX idx_purchase_lots_remaining ON purchase_lots(sku, quantity_remaining) WHERE quantity_remaining > 0;
CREATE INDEX idx_purchase_lots_date ON purchase_lots(purchase_date DESC);

-- ============================================================================
-- CUSTOMERS AND SALES
-- ============================================================================

-- B2B customers
CREATE TABLE customers (
    customer_id     SERIAL PRIMARY KEY,
    company_name    VARCHAR(200) NOT NULL,
    contact_person  VARCHAR(100),
    email           VARCHAR(100),
    phone           VARCHAR(50),
    created_at      TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

COMMENT ON TABLE customers IS 'B2B customer information';

CREATE INDEX idx_customers_company ON customers(company_name);

-- Sales invoices (always in UAH)
CREATE TABLE invoices (
    invoice_id      SERIAL PRIMARY KEY,
    customer_id     INTEGER NOT NULL REFERENCES customers(customer_id),
    invoice_date    DATE NOT NULL,
    total_amount    DECIMAL(15, 2) NOT NULL CHECK (total_amount >= 0),
    notes           TEXT,
    created_at      TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

COMMENT ON TABLE invoices IS 'Sales invoices to customers (always in UAH)';

CREATE INDEX idx_invoices_date ON invoices(invoice_date DESC);
CREATE INDEX idx_invoices_customer ON invoices(customer_id);

-- Invoice line items
CREATE TABLE invoice_lines (
    invoice_line_id SERIAL PRIMARY KEY,
    invoice_id      INTEGER NOT NULL REFERENCES invoices(invoice_id) ON DELETE CASCADE,
    sku             VARCHAR(50) NOT NULL REFERENCES heater_models(sku),
    quantity_sold   INTEGER NOT NULL CHECK (quantity_sold > 0),
    unit_price_uah  DECIMAL(15, 2) NOT NULL CHECK (unit_price_uah >= 0),
    created_at      TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

COMMENT ON TABLE invoice_lines IS 'Line items for sales invoices';

CREATE INDEX idx_invoice_lines_invoice ON invoice_lines(invoice_id);
CREATE INDEX idx_invoice_lines_sku ON invoice_lines(sku);
CREATE INDEX idx_invoice_lines_sku_date ON invoice_lines(sku, invoice_id);

-- ============================================================================
-- VIEWS FOR REPORTING
-- ============================================================================

-- Current stock levels with weighted average costs
CREATE VIEW v_current_stock AS
SELECT
    l.sku,
    h.model_name,
    h.manufacturer,
    SUM(l.quantity_remaining) as total_in_stock,
    COUNT(DISTINCT l.lot_id) as lot_count,
    -- Weighted average cost in original currency (will need conversion)
    SUM(l.quantity_remaining * l.unit_price_original) /
        NULLIF(SUM(l.quantity_remaining), 0) as weighted_avg_cost_original
FROM purchase_lots l
JOIN heater_models h ON l.sku = h.sku
WHERE l.quantity_remaining > 0
GROUP BY l.sku, h.model_name, h.manufacturer;

COMMENT ON VIEW v_current_stock IS 'Aggregated inventory levels by SKU with weighted average purchase costs';

-- Latest exchange rate for each currency pair
CREATE VIEW v_current_exchange_rates AS
SELECT DISTINCT ON (from_currency, to_currency)
    from_currency,
    to_currency,
    rate_date,
    exchange_rate
FROM exchange_rates
ORDER BY from_currency, to_currency, rate_date DESC;

COMMENT ON VIEW v_current_exchange_rates IS 'Most recent exchange rate for each currency pair';

-- Current list prices
CREATE VIEW v_current_list_prices AS
SELECT
    sku,
    list_price_uah,
    effective_date
FROM list_prices
WHERE is_current = true;

COMMENT ON VIEW v_current_list_prices IS 'Currently active list prices for all models';

-- ============================================================================
-- HELPER FUNCTIONS
-- ============================================================================

-- Function to get exchange rate for a specific date (or most recent before)
CREATE OR REPLACE FUNCTION get_exchange_rate(
    p_from_currency CHAR(3),
    p_to_currency CHAR(3),
    p_date DATE
) RETURNS DECIMAL(15, 6) AS $$
DECLARE
    v_rate DECIMAL(15, 6);
BEGIN
    -- Handle same currency
    IF p_from_currency = p_to_currency THEN
        RETURN 1.0;
    END IF;

    -- Get most recent rate on or before the specified date
    SELECT exchange_rate INTO v_rate
    FROM exchange_rates
    WHERE from_currency = p_from_currency
      AND to_currency = p_to_currency
      AND rate_date <= p_date
    ORDER BY rate_date DESC
    LIMIT 1;

    IF v_rate IS NULL THEN
        RAISE EXCEPTION 'No exchange rate found for % to % on or before %',
            p_from_currency, p_to_currency, p_date;
    END IF;

    RETURN v_rate;
END;
$$ LANGUAGE plpgsql STABLE;

COMMENT ON FUNCTION get_exchange_rate IS 'Returns exchange rate for a date, using most recent available if exact date not found';

-- ============================================================================
-- STATISTICS CONFIGURATION
-- ============================================================================

-- Increase statistics target for columns used in complex queries
ALTER TABLE exchange_rates ALTER COLUMN rate_date SET STATISTICS 1000;
ALTER TABLE purchase_lots ALTER COLUMN quantity_remaining SET STATISTICS 1000;
ALTER TABLE invoice_lines ALTER COLUMN quantity_sold SET STATISTICS 1000;

-- ============================================================================
-- SAMPLE QUERY OPTIMIZATION SCENARIOS
-- ============================================================================

COMMENT ON TABLE exchange_rates IS
'Exchange rates table is designed to demonstrate:
1. Temporal query optimization with backward-looking rate resolution
2. Composite index usage for multi-column lookups
3. Impact of statistics on date range queries';

COMMENT ON TABLE purchase_lots IS
'Purchase lots table demonstrates:
1. Partial index optimization (WHERE quantity_remaining > 0)
2. Aggregation query performance with proper indexing
3. JOIN optimization with foreign key indexes
4. Cardinality changes affecting query plans as inventory is depleted';

COMMENT ON TABLE invoice_lines IS
'Invoice lines table demonstrates:
1. Time-series query patterns with date filtering
2. Aggregation performance across large datasets
3. Query plan changes as table grows over time';
