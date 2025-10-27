#!/usr/bin/env python3
"""
Generate seed data for HotWind database with one year of historical data.
Creates realistic data including exchange rates, purchases, and sales.
"""

import random
import math
from datetime import datetime, date, timedelta
from decimal import Decimal, ROUND_HALF_UP

# Configuration
START_DATE = date(2024, 1, 1)
END_DATE = date(2024, 12, 31)
DAYS = (END_DATE - START_DATE).days + 1

# Reference data
COUNTRIES = [
    ('US', 'United States'),
    ('DE', 'Germany'),
    ('CN', 'China'),
    ('PL', 'Poland'),
    ('UA', 'Ukraine'),
]

CURRENCIES = [
    ('UAH', 'Ukrainian Hryvnia', '₴'),
    ('USD', 'US Dollar', '$'),
    ('EUR', 'Euro', '€'),
    ('CNY', 'Chinese Yuan', '¥'),
    ('PLN', 'Polish Zloty', 'zł'),
]

# Initial exchange rates (UAH per unit of foreign currency)
INITIAL_RATES = {
    ('USD', 'UAH'): 38.50,
    ('EUR', 'UAH'): 42.30,
    ('CNY', 'UAH'): 5.40,
    ('PLN', 'UAH'): 9.80,
}

# Vendors
VENDORS = [
    ('ThermoTech USA Inc.', 'US', 'USD', 'contact@thermotech-usa.com'),
    ('HeizKraft GmbH', 'DE', 'EUR', 'info@heizkraft.de'),
    ('Warmth Industries Ltd.', 'CN', 'CNY', 'sales@warmth.cn'),
    ('Polski Grzewczy Sp. z o.o.', 'PL', 'PLN', 'biuro@polskigrzewczy.pl'),
]

# Heater models with manufacturer, capacity, and price ranges
HEATER_MODELS = [
    # High-end industrial (Bosch)
    ('BOSCH-IH-5000', 'Bosch Industrial Heater 5000', 'Bosch', 50.0, (25000, 30000)),
    ('BOSCH-IH-7500', 'Bosch Industrial Heater 7500', 'Bosch', 75.0, (35000, 42000)),
    ('BOSCH-IH-10K', 'Bosch Industrial Heater 10000', 'Bosch', 100.0, (48000, 55000)),

    # Mid-range (Carrier)
    ('CARRIER-CH-3000', 'Carrier Commercial Heater 3000', 'Carrier', 30.0, (18000, 22000)),
    ('CARRIER-CH-4500', 'Carrier Commercial Heater 4500', 'Carrier', 45.0, (24000, 28000)),
    ('CARRIER-CH-6000', 'Carrier Commercial Heater 6000', 'Carrier', 60.0, (32000, 38000)),

    # Premium Asian (Mitsubishi)
    ('MITS-HE-2500', 'Mitsubishi Heavy Electric 2500', 'Mitsubishi', 25.0, (15000, 18000)),
    ('MITS-HE-5000', 'Mitsubishi Heavy Electric 5000', 'Mitsubishi', 50.0, (28000, 33000)),
    ('MITS-HE-8000', 'Mitsubishi Heavy Electric 8000', 'Mitsubishi', 80.0, (42000, 48000)),

    # Budget-friendly (Generic Chinese)
    ('HW-EC-2000', 'HeatWave EconoMax 2000', 'HeatWave', 20.0, (8000, 12000)),
    ('HW-EC-3500', 'HeatWave EconoMax 3500', 'HeatWave', 35.0, (14000, 18000)),
    ('HW-EC-5000', 'HeatWave EconoMax 5000', 'HeatWave', 50.0, (20000, 25000)),

    # Polish brands
    ('PG-TH-3000', 'PolGrzew ThermoMax 3000', 'PolGrzew', 30.0, (16000, 20000)),
    ('PG-TH-4000', 'PolGrzew ThermoMax 4000', 'PolGrzew', 40.0, (22000, 26000)),

    # Specialized high-power
    ('BOSCH-IP-15K', 'Bosch Industrial Pro 15000', 'Bosch', 150.0, (68000, 78000)),
    ('CARRIER-HP-12K', 'Carrier HeavyPower 12000', 'Carrier', 120.0, (58000, 65000)),

    # Compact units
    ('MITS-CM-1500', 'Mitsubishi Compact 1500', 'Mitsubishi', 15.0, (10000, 13000)),
    ('BOSCH-CM-1800', 'Bosch Compact 1800', 'Bosch', 18.0, (12000, 15000)),

    # Energy efficient
    ('CARRIER-EE-3500', 'Carrier EcoEfficient 3500', 'Carrier', 35.0, (26000, 30000)),
    ('MITS-EE-4000', 'Mitsubishi EcoEfficient 4000', 'Mitsubishi', 40.0, (28000, 32000)),

    # Industrial heavy-duty
    ('BOSCH-HD-20K', 'Bosch HeavyDuty 20000', 'Bosch', 200.0, (88000, 98000)),
    ('CARRIER-HD-18K', 'Carrier HeavyDuty 18000', 'Carrier', 180.0, (78000, 88000)),

    # Budget options
    ('HW-BS-1500', 'HeatWave BasicStar 1500', 'HeatWave', 15.0, (6000, 9000)),
    ('HW-BS-2500', 'HeatWave BasicStar 2500', 'HeatWave', 25.0, (10000, 14000)),
    ('PG-ST-2000', 'PolGrzew Standard 2000', 'PolGrzew', 20.0, (11000, 14000)),

    # Advanced models
    ('BOSCH-SM-8000', 'Bosch SmartMax 8000', 'Bosch', 80.0, (45000, 52000)),
    ('MITS-SM-7000', 'Mitsubishi SmartMax 7000', 'Mitsubishi', 70.0, (38000, 44000)),
    ('CARRIER-SM-9000', 'Carrier SmartMax 9000', 'Carrier', 90.0, (48000, 55000)),
]

# Customer companies
CUSTOMER_COMPANIES = [
    ('Kyiv Manufacturing Corp.', 'Oleksandr Petrenko', 'o.petrenko@kyivmfg.ua', '+380441234567'),
    ('Lviv Industrial Solutions', 'Natalia Kovalenko', 'n.kovalenko@lvivind.ua', '+380322345678'),
    ('Odesa Logistics Hub', 'Viktor Shevchenko', 'v.shevchenko@odesalog.ua', '+380482456789'),
    ('Kharkiv Engineering Ltd.', 'Iryna Bondarenko', 'i.bondarenko@kharkiveng.ua', '+380573567890'),
    ('Dnipro Heavy Industries', 'Andriy Kravchenko', 'a.kravchenko@dnipro-heavy.ua', '+380562678901'),
    ('Zaporizhzhia Steel Works', 'Oksana Moroz', 'o.moroz@zapsteel.ua', '+380612789012'),
    ('Poltava Agricultural Systems', 'Dmytro Tkachenko', 'd.tkachenko@poltavaagri.ua', '+380532890123'),
    ('Chernihiv Processing Plant', 'Yulia Lysenko', 'y.lysenko@chernihivproc.ua', '+380462901234'),
    ('Vinnytsia Food Industries', 'Sergiy Koval', 's.koval@vinnytsiafood.ua', '+380432012345'),
    ('Zhytomyr Construction Group', 'Tetiana Savchenko', 't.savchenko@zhytomyrconstr.ua', '+380412123456'),
    ('Rivne Energy Systems', 'Maksym Polishchuk', 'm.polishchuk@rivneenergy.ua', '+380362234567'),
    ('Ternopil Manufacturing Hub', 'Olena Melnyk', 'o.melnyk@ternopilmfg.ua', '+380352345678'),
    ('Ivano-Frankivsk Industries', 'Roman Boyko', 'r.boyko@ifind.ua', '+380342456789'),
    ('Lutsk Production Facility', 'Marina Koval', 'm.koval@lutskprod.ua', '+380332567890'),
    ('Uzhhorod Border Logistics', 'Vasyl Horvat', 'v.horvat@uzhlog.ua', '+380312678901'),
]


def geometric_brownian_motion(initial_value, days, mu=0.0001, sigma=0.015):
    """Generate exchange rates using Geometric Brownian Motion."""
    rates = [initial_value]
    for _ in range(days - 1):
        dt = 1.0
        z = random.gauss(0, 1)
        rate_change = (mu - 0.5 * sigma ** 2) * dt + sigma * math.sqrt(dt) * z
        new_rate = rates[-1] * math.exp(rate_change)
        rates.append(new_rate)
    return rates


def round_decimal(value, places=2):
    """Round decimal to specified places."""
    quantizer = Decimal(10) ** -places
    return Decimal(str(value)).quantize(quantizer, rounding=ROUND_HALF_UP)


def sql_escape(text):
    """Escape single quotes for SQL."""
    if text is None:
        return 'NULL'
    return text.replace("'", "''")


def generate_sql():
    """Generate complete SQL seed data."""
    output = []
    output.append("-- HotWind Seed Data")
    output.append("-- Generated: " + datetime.now().isoformat())
    output.append("-- Period: {} to {}".format(START_DATE, END_DATE))
    output.append("")
    output.append("BEGIN;")
    output.append("")

    # Countries
    output.append("-- Countries")
    for code, name in COUNTRIES:
        output.append(f"INSERT INTO countries (country_code, country_name) VALUES ('{code}', '{sql_escape(name)}');")
    output.append("")

    # Currencies
    output.append("-- Currencies")
    for code, name, symbol in CURRENCIES:
        output.append(f"INSERT INTO currencies (currency_code, currency_name, symbol) VALUES ('{code}', '{sql_escape(name)}', '{sql_escape(symbol)}');")
    output.append("")

    # Generate exchange rates
    output.append("-- Exchange Rates (1 year, daily)")
    rates_data = {}
    for (from_curr, to_curr), initial_rate in INITIAL_RATES.items():
        rates = geometric_brownian_motion(initial_rate, DAYS, mu=0.0001, sigma=0.015)
        rates_data[(from_curr, to_curr)] = rates

    for (from_curr, to_curr), rates in rates_data.items():
        for i, rate in enumerate(rates):
            current_date = START_DATE + timedelta(days=i)
            rate_rounded = round_decimal(rate, 6)
            output.append(
                f"INSERT INTO exchange_rates (from_currency, to_currency, rate_date, exchange_rate) "
                f"VALUES ('{from_curr}', '{to_curr}', '{current_date}', {rate_rounded});"
            )
    output.append("")

    # Heater models
    output.append("-- Heater Models")
    for sku, model_name, manufacturer, capacity, _ in HEATER_MODELS:
        output.append(
            f"INSERT INTO heater_models (sku, model_name, manufacturer, capacity_kw) "
            f"VALUES ('{sku}', '{sql_escape(model_name)}', '{sql_escape(manufacturer)}', {capacity});"
        )
    output.append("")

    # List prices (current prices based on price ranges)
    output.append("-- List Prices (current)")
    for sku, _, _, _, price_range in HEATER_MODELS:
        list_price = round_decimal(random.uniform(*price_range), 2)
        output.append(
            f"INSERT INTO list_prices (sku, list_price_uah, effective_date, is_current) "
            f"VALUES ('{sku}', {list_price}, '{END_DATE}', true);"
        )
    output.append("")

    # Vendors
    output.append("-- Vendors")
    for i, (name, country, currency, contact) in enumerate(VENDORS, 1):
        output.append(
            f"INSERT INTO vendors (vendor_id, vendor_name, country_code, currency_code, contact_info) "
            f"VALUES ({i}, '{sql_escape(name)}', '{country}', '{currency}', '{sql_escape(contact)}');"
        )
    output.append("")

    # Customers
    output.append("-- Customers")
    for i, (company, contact, email, phone) in enumerate(CUSTOMER_COMPANIES, 1):
        output.append(
            f"INSERT INTO customers (customer_id, company_name, contact_person, email, phone) "
            f"VALUES ({i}, '{sql_escape(company)}', '{sql_escape(contact)}', '{sql_escape(email)}', '{sql_escape(phone)}');"
        )
    output.append("")

    # Purchase Orders and Lots
    output.append("-- Purchase Orders and Lots")
    po_id = 1
    lot_id = 1

    # Spread purchases throughout the year (roughly weekly)
    num_purchases = 80
    purchase_dates = sorted([START_DATE + timedelta(days=random.randint(0, DAYS - 1)) for _ in range(num_purchases)])

    for po_date in purchase_dates:
        vendor_id = random.randint(1, len(VENDORS))
        vendor_currency = VENDORS[vendor_id - 1][2]

        # Each PO has 1-4 line items
        num_items = random.randint(1, 4)
        po_total = Decimal('0')

        output.append(f"-- PO #{po_id} on {po_date}")

        selected_models = random.sample(HEATER_MODELS, min(num_items, len(HEATER_MODELS)))

        for sku, _, _, _, price_range in selected_models:
            # Purchase price in vendor currency
            if vendor_currency == 'USD':
                unit_price = round_decimal(random.uniform(*price_range) / 40, 2)
            elif vendor_currency == 'EUR':
                unit_price = round_decimal(random.uniform(*price_range) / 45, 2)
            elif vendor_currency == 'CNY':
                unit_price = round_decimal(random.uniform(*price_range) / 5.5, 2)
            elif vendor_currency == 'PLN':
                unit_price = round_decimal(random.uniform(*price_range) / 10, 2)
            else:
                unit_price = round_decimal(random.uniform(*price_range), 2)

            quantity = random.choice([5, 10, 15, 20, 25, 30, 40, 50])
            lot_number = f"LOT-{po_date.year}{po_date.month:02d}{po_date.day:02d}-{lot_id:04d}"

            po_total += unit_price * quantity

            output.append(
                f"INSERT INTO purchase_lots (lot_id, po_id, sku, lot_number, quantity_purchased, "
                f"quantity_remaining, unit_price_original, purchase_date) "
                f"VALUES ({lot_id}, {po_id}, '{sku}', '{lot_number}', {quantity}, {quantity}, "
                f"{unit_price}, '{po_date}');"
            )

            lot_id += 1

        output.append(
            f"INSERT INTO purchase_orders (po_id, vendor_id, po_date, total_amount) "
            f"VALUES ({po_id}, {vendor_id}, '{po_date}', {round_decimal(po_total, 2)});"
        )
        output.append("")

        po_id += 1

    # Invoices
    # Generate sales throughout the year (more frequent than purchases)
    output.append("-- Sales Invoices and Lines")
    invoice_id = 1
    invoice_line_id = 1

    num_invoices = 300
    invoice_dates = sorted([START_DATE + timedelta(days=random.randint(7, DAYS - 1)) for _ in range(num_invoices)])

    for invoice_date in invoice_dates:
        customer_id = random.randint(1, len(CUSTOMER_COMPANIES))

        # Each invoice has 1-3 line items
        num_items = random.randint(1, 3)
        invoice_total = Decimal('0')

        selected_models = random.sample(HEATER_MODELS, min(num_items, len(HEATER_MODELS)))

        for sku, _, _, _, price_range in selected_models:
            # Sale price in UAH (based on list price with some variation)
            base_price = round_decimal(random.uniform(*price_range), 2)
            multiplier = Decimal(str(random.uniform(0.95, 1.05)))
            unit_price = round_decimal(float(base_price) * float(multiplier), 2)

            quantity = random.choice([1, 2, 3, 5, 8, 10])

            invoice_total += unit_price * quantity

            output.append(
                f"INSERT INTO invoice_lines (invoice_line_id, invoice_id, sku, quantity_sold, unit_price_uah) "
                f"VALUES ({invoice_line_id}, {invoice_id}, '{sku}', {quantity}, {unit_price});"
            )

            invoice_line_id += 1

        output.append(
            f"INSERT INTO invoices (invoice_id, customer_id, invoice_date, total_amount) "
            f"VALUES ({invoice_id}, {customer_id}, '{invoice_date}', {round_decimal(invoice_total, 2)});"
        )

        invoice_id += 1

        # Add spacing every 20 invoices for readability
        if invoice_id % 20 == 0:
            output.append("")

    output.append("")
    output.append("-- Update sequences to current values")
    output.append(f"SELECT setval('vendors_vendor_id_seq', {len(VENDORS)});")
    output.append(f"SELECT setval('customers_customer_id_seq', {len(CUSTOMER_COMPANIES)});")
    output.append(f"SELECT setval('purchase_orders_po_id_seq', {po_id - 1});")
    output.append(f"SELECT setval('purchase_lots_lot_id_seq', {lot_id - 1});")
    output.append(f"SELECT setval('invoices_invoice_id_seq', {invoice_id - 1});")
    output.append(f"SELECT setval('invoice_lines_invoice_line_id_seq', {invoice_line_id - 1});")
    output.append("")
    output.append("COMMIT;")
    output.append("")
    output.append("-- Analyze tables for query optimization")
    output.append("ANALYZE;")

    return '\n'.join(output)


if __name__ == '__main__':
    sql = generate_sql()
    print(sql)
