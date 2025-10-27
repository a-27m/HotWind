using HotWind.Api.Models.Domain;
using Npgsql;

namespace HotWind.Api.Data.Repositories;

public class ExchangeRateRepository : IExchangeRateRepository
{
    private readonly NpgsqlDataSource _dataSource;

    public ExchangeRateRepository(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task<decimal> GetRateAsync(string fromCurrency, string toCurrency, DateOnly date)
    {
        // Handle same currency
        if (fromCurrency == toCurrency)
        {
            return 1.0m;
        }

        // Get most recent rate on or before the specified date (backward-looking)
        const string sql = @"
            SELECT exchange_rate
            FROM exchange_rates
            WHERE from_currency = $1 AND to_currency = $2 AND rate_date <= $3
            ORDER BY rate_date DESC
            LIMIT 1";

        await using var conn = await _dataSource.OpenConnectionAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue(fromCurrency);
        cmd.Parameters.AddWithValue(toCurrency);
        cmd.Parameters.AddWithValue(date);

        var result = await cmd.ExecuteScalarAsync();

        if (result == null)
        {
            throw new InvalidOperationException(
                $"No exchange rate found for {fromCurrency} to {toCurrency} on or before {date}");
        }

        return (decimal)result;
    }

    public async Task<bool> RateExistsAsync(string fromCurrency, string toCurrency, DateOnly date)
    {
        const string sql = @"
            SELECT COUNT(*)
            FROM exchange_rates
            WHERE from_currency = $1 AND to_currency = $2 AND rate_date = $3";

        await using var conn = await _dataSource.OpenConnectionAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue(fromCurrency);
        cmd.Parameters.AddWithValue(toCurrency);
        cmd.Parameters.AddWithValue(date);

        var count = await cmd.ExecuteScalarAsync();
        return count != null && Convert.ToInt64(count) > 0;
    }

    public async Task<int> InsertRatesAsync(List<ExchangeRate> rates)
    {
        if (rates.Count == 0)
        {
            return 0;
        }

        const string sql = @"
            INSERT INTO exchange_rates (from_currency, to_currency, rate_date, exchange_rate)
            VALUES ($1, $2, $3, $4)
            ON CONFLICT (from_currency, to_currency, rate_date) DO NOTHING";

        int inserted = 0;

        await using var conn = await _dataSource.OpenConnectionAsync();
        await using var transaction = await conn.BeginTransactionAsync();

        try
        {
            foreach (var rate in rates)
            {
                await using var cmd = new NpgsqlCommand(sql, conn, transaction);
                cmd.Parameters.AddWithValue(rate.FromCurrency);
                cmd.Parameters.AddWithValue(rate.ToCurrency);
                cmd.Parameters.AddWithValue(rate.RateDate);
                cmd.Parameters.AddWithValue(rate.Rate);

                inserted += await cmd.ExecuteNonQueryAsync();
            }

            await transaction.CommitAsync();
            return inserted;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<List<string>> GetAvailableCurrenciesAsync()
    {
        const string sql = @"
            SELECT currency_code
            FROM currencies
            WHERE currency_code != 'UAH'
            ORDER BY currency_code";

        var currencies = new List<string>();

        await using var conn = await _dataSource.OpenConnectionAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            currencies.Add(reader.GetString(0));
        }

        return currencies;
    }
}
