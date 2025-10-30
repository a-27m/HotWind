using HotWind.Api.Models.Dtos;
using Npgsql;

namespace HotWind.Api.Data.Repositories;

public class ReportRepository : IReportRepository
{
    private readonly NpgsqlDataSource _dataSource;

    public ReportRepository(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task<List<StockReportItemDto>> GetStockReportAsync()
    {
        // Complex query demonstrating:
        // 1. CTEs for multi-step calculations
        // 2. LATERAL joins for temporal exchange rate lookups
        // 3. Aggregations with weighted averages
        // 4. Multiple table joins
        const string sql = @"
            WITH lot_details AS (
                SELECT
                    pl.sku,
                    pl.quantity_remaining,
                    pl.unit_price_original,
                    v.currency_code,
                    pl.purchase_date,
                    pl.lot_id
                FROM purchase_lots pl
                JOIN purchase_orders po ON pl.po_id = po.po_id
                JOIN vendors v ON po.vendor_id = v.vendor_id
                WHERE pl.quantity_remaining > 0
            ),
            lot_values AS (
                SELECT
                    ld.sku,
                    ld.quantity_remaining,
                    ld.unit_price_original,
                    ld.currency_code,
                    er.exchange_rate,
                    (ld.unit_price_original * er.exchange_rate) as unit_price_uah
                FROM lot_details ld
                CROSS JOIN LATERAL (
                    SELECT exchange_rate
                    FROM exchange_rates
                    WHERE from_currency = ld.currency_code
                      AND to_currency = 'UAH'
                      AND rate_date <= CURRENT_DATE
                    ORDER BY rate_date DESC
                    LIMIT 1
                ) er
            )
            SELECT
                lv.sku,
                hm.model_name,
                hm.manufacturer,
                SUM(lv.quantity_remaining)::int as stock_level,
                COUNT(DISTINCT lv.sku)::int as lot_count,
                (SUM(lv.quantity_remaining * lv.unit_price_uah) /
                    NULLIF(SUM(lv.quantity_remaining), 0))::numeric(15,2) as weighted_avg_purchase_price_uah,
                lp.list_price_uah,
                (lp.list_price_uah * SUM(lv.quantity_remaining) -
                    SUM(lv.quantity_remaining * lv.unit_price_uah))::numeric(15,2) as potential_profit,
                (CASE WHEN SUM(lv.quantity_remaining * lv.unit_price_uah) > 0
                    THEN ((lp.list_price_uah * SUM(lv.quantity_remaining) -
                           SUM(lv.quantity_remaining * lv.unit_price_uah)) /
                          SUM(lv.quantity_remaining * lv.unit_price_uah) * 100)
                    ELSE 0
                 END)::numeric(15,2) as profit_margin_percent
            FROM lot_values lv
            JOIN heater_models hm ON lv.sku = hm.sku
            LEFT JOIN list_prices lp ON lv.sku = lp.sku AND lp.is_current = true
            GROUP BY lv.sku, hm.model_name, hm.manufacturer, lp.list_price_uah
            ORDER BY lv.sku";

        var items = new List<StockReportItemDto>();

        await using var conn = await _dataSource.OpenConnectionAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            items.Add(new StockReportItemDto
            {
                Sku = reader.GetString(0),
                ModelName = reader.GetString(1),
                Manufacturer = reader.GetString(2),
                StockLevel = reader.GetInt32(3),
                LotCount = reader.GetInt32(4),
                WeightedAvgPurchasePriceUah = reader.GetDecimal(5),
                ListPriceUah = reader.IsDBNull(6) ? 0 : reader.GetDecimal(6),
                PotentialProfit = reader.GetDecimal(7),
                ProfitMarginPercent = reader.GetDecimal(8)
            });
        }

        return items;
    }

    public async Task<List<PriceListReportItemDto>> GetPriceListReportAsync()
    {
        // Query demonstrating:
        // 1. Weighted lot value calculations
        // 2. Comparison between historical cost and current market value
        // 3. Exchange rate application for multi-currency purchases
        const string sql = @"
            WITH lot_details AS (
                SELECT
                    pl.sku,
                    pl.quantity_remaining,
                    pl.unit_price_original,
                    v.currency_code
                FROM purchase_lots pl
                JOIN purchase_orders po ON pl.po_id = po.po_id
                JOIN vendors v ON po.vendor_id = v.vendor_id
                WHERE pl.quantity_remaining > 0
            ),
            lot_values_uah AS (
                SELECT
                    ld.sku,
                    ld.quantity_remaining,
                    (ld.unit_price_original * er.exchange_rate) as unit_price_uah
                FROM lot_details ld
                CROSS JOIN LATERAL (
                    SELECT exchange_rate
                    FROM exchange_rates
                    WHERE from_currency = ld.currency_code
                      AND to_currency = 'UAH'
                      AND rate_date <= CURRENT_DATE
                    ORDER BY rate_date DESC
                    LIMIT 1
                ) er
            )
            SELECT
                lv.sku,
                hm.model_name,
                hm.manufacturer,
                SUM(lv.quantity_remaining)::int as stock_level,
                SUM(lv.quantity_remaining * lv.unit_price_uah)::numeric(15,2) as weighted_lot_value_uah,
                (lp.list_price_uah * SUM(lv.quantity_remaining))::numeric(15,2) as current_market_value_uah,
                ((lp.list_price_uah * SUM(lv.quantity_remaining)) -
                 SUM(lv.quantity_remaining * lv.unit_price_uah))::numeric(15,2) as value_difference_uah,
                (CASE WHEN SUM(lv.quantity_remaining * lv.unit_price_uah) > 0
                    THEN (((lp.list_price_uah * SUM(lv.quantity_remaining)) -
                           SUM(lv.quantity_remaining * lv.unit_price_uah)) /
                          SUM(lv.quantity_remaining * lv.unit_price_uah) * 100)
                    ELSE 0
                 END)::numeric(15,2) as value_difference_percent
            FROM lot_values_uah lv
            JOIN heater_models hm ON lv.sku = hm.sku
            LEFT JOIN list_prices lp ON lv.sku = lp.sku AND lp.is_current = true
            GROUP BY lv.sku, hm.model_name, hm.manufacturer, lp.list_price_uah
            ORDER BY value_difference_uah DESC";

        var items = new List<PriceListReportItemDto>();

        await using var conn = await _dataSource.OpenConnectionAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            items.Add(new PriceListReportItemDto
            {
                Sku = reader.GetString(0),
                ModelName = reader.GetString(1),
                Manufacturer = reader.GetString(2),
                StockLevel = reader.GetInt32(3),
                WeightedLotValueUah = reader.GetDecimal(4),
                CurrentMarketValueUah = reader.IsDBNull(5) ? 0 : reader.GetDecimal(5),
                ValueDifferenceUah = reader.GetDecimal(6),
                ValueDifferencePercent = reader.GetDecimal(7)
            });
        }

        return items;
    }

    public async Task<List<CurrencyTranslationReportItemDto>> GetCurrencyTranslationReportAsync(
        DateOnly startDate, DateOnly endDate)
    {
        // Most complex query demonstrating:
        // 1. Time-series data analysis with date ranges
        // 2. Historical vs current exchange rate comparison
        // 3. Multiple CTEs for step-by-step calculation
        // 4. Impact of currency fluctuations on revenue
        const string sql = @"
            WITH sales_in_period AS (
                SELECT
                    il.sku,
                    il.quantity_sold,
                    il.unit_price_uah,
                    i.invoice_date
                FROM invoice_lines il
                JOIN invoices i ON il.invoice_id = i.invoice_id
                WHERE i.invoice_date BETWEEN $1 AND $2
            ),
            lot_purchase_info AS (
                SELECT DISTINCT ON (sip.sku, sip.invoice_date)
                    sip.sku,
                    sip.quantity_sold,
                    sip.unit_price_uah,
                    sip.invoice_date,
                    pl.unit_price_original,
                    v.currency_code
                FROM sales_in_period sip
                LEFT JOIN purchase_lots pl ON sip.sku = pl.sku
                LEFT JOIN purchase_orders po ON pl.po_id = po.po_id
                LEFT JOIN vendors v ON po.vendor_id = v.vendor_id
                ORDER BY sip.sku, sip.invoice_date, pl.purchase_date ASC
            ),
            sales_with_rates AS (
                SELECT
                    lpi.sku,
                    lpi.quantity_sold,
                    lpi.unit_price_uah,
                    lpi.unit_price_original,
                    lpi.currency_code,
                    hist_er.exchange_rate as historical_rate,
                    curr_er.exchange_rate as current_rate
                FROM lot_purchase_info lpi
                LEFT JOIN LATERAL (
                    SELECT exchange_rate
                    FROM exchange_rates
                    WHERE from_currency = COALESCE(lpi.currency_code, 'UAH')
                      AND to_currency = 'UAH'
                      AND rate_date <= lpi.invoice_date
                    ORDER BY rate_date DESC
                    LIMIT 1
                ) hist_er ON true
                LEFT JOIN LATERAL (
                    SELECT exchange_rate
                    FROM exchange_rates
                    WHERE from_currency = COALESCE(lpi.currency_code, 'UAH')
                      AND to_currency = 'UAH'
                      AND rate_date <= CURRENT_DATE
                    ORDER BY rate_date DESC
                    LIMIT 1
                ) curr_er ON true
            )
            SELECT
                swr.sku,
                hm.model_name,
                hm.manufacturer,
                SUM(swr.quantity_sold)::int as total_units_sold,
                SUM(swr.unit_price_uah * swr.quantity_sold)::numeric(15,2) as historical_value_uah,
                (CASE
                    WHEN swr.currency_code IS NOT NULL AND swr.unit_price_original IS NOT NULL
                    THEN SUM(swr.unit_price_original * swr.current_rate * swr.quantity_sold)
                    ELSE SUM(swr.unit_price_uah * swr.quantity_sold)
                 END)::numeric(15,2) as current_value_uah,
                (CASE
                    WHEN swr.currency_code IS NOT NULL AND swr.unit_price_original IS NOT NULL
                    THEN (SUM(swr.unit_price_original * swr.current_rate * swr.quantity_sold) -
                          SUM(swr.unit_price_uah * swr.quantity_sold))
                    ELSE 0
                 END)::numeric(15,2) as value_difference_uah,
                (CASE
                    WHEN swr.currency_code IS NOT NULL AND SUM(swr.unit_price_uah * swr.quantity_sold) > 0
                    THEN ((SUM(swr.unit_price_original * swr.current_rate * swr.quantity_sold) -
                           SUM(swr.unit_price_uah * swr.quantity_sold)) /
                          SUM(swr.unit_price_uah * swr.quantity_sold) * 100)
                    ELSE 0
                 END)::numeric(15,2) as exchange_rate_impact_percent
            FROM sales_with_rates swr
            JOIN heater_models hm ON swr.sku = hm.sku
            GROUP BY swr.sku, hm.model_name, hm.manufacturer, swr.currency_code, swr.unit_price_original
            HAVING SUM(swr.quantity_sold) > 0
            ORDER BY total_units_sold DESC";

        var items = new List<CurrencyTranslationReportItemDto>();

        await using var conn = await _dataSource.OpenConnectionAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue(startDate);
        cmd.Parameters.AddWithValue(endDate);

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            items.Add(new CurrencyTranslationReportItemDto
            {
                Sku = reader.GetString(0),
                ModelName = reader.GetString(1),
                Manufacturer = reader.GetString(2),
                TotalUnitsSold = reader.GetInt32(3),
                HistoricalValueUah = reader.GetDecimal(4),
                CurrentValueUah = reader.GetDecimal(5),
                ValueDifferenceUah = reader.GetDecimal(6),
                ExchangeRateImpactPercent = reader.GetDecimal(7)
            });
        }

        return items;
    }
}
