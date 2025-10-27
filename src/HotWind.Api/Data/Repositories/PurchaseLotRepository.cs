using HotWind.Api.Models.Domain;
using Npgsql;

namespace HotWind.Api.Data.Repositories;

public class PurchaseLotRepository : IPurchaseLotRepository
{
    private readonly NpgsqlDataSource _dataSource;

    public PurchaseLotRepository(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task<List<PurchaseLot>> GetAvailableLotsBySkuAsync(string sku)
    {
        // FIFO: Order by purchase_date ASC to get oldest lots first
        const string sql = @"
            SELECT lot_id, po_id, sku, lot_number, quantity_purchased, quantity_remaining,
                   unit_price_original, purchase_date, created_at
            FROM purchase_lots
            WHERE sku = $1 AND quantity_remaining > 0
            ORDER BY purchase_date ASC, lot_id ASC";

        var lots = new List<PurchaseLot>();

        await using var conn = await _dataSource.OpenConnectionAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue(sku);

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            lots.Add(MapFromReader(reader));
        }

        return lots;
    }

    public async Task UpdateQuantityRemainingAsync(int lotId, int newQuantity)
    {
        const string sql = @"
            UPDATE purchase_lots
            SET quantity_remaining = $2
            WHERE lot_id = $1";

        await using var conn = await _dataSource.OpenConnectionAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue(lotId);
        cmd.Parameters.AddWithValue(newQuantity);

        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<int> GetTotalStockBySkuAsync(string sku)
    {
        const string sql = @"
            SELECT COALESCE(SUM(quantity_remaining), 0)
            FROM purchase_lots
            WHERE sku = $1";

        await using var conn = await _dataSource.OpenConnectionAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue(sku);

        var result = await cmd.ExecuteScalarAsync();
        return result != null ? Convert.ToInt32(result) : 0;
    }

    private static PurchaseLot MapFromReader(NpgsqlDataReader reader)
    {
        return new PurchaseLot
        {
            LotId = reader.GetInt32(0),
            PoId = reader.GetInt32(1),
            Sku = reader.GetString(2),
            LotNumber = reader.GetString(3),
            QuantityPurchased = reader.GetInt32(4),
            QuantityRemaining = reader.GetInt32(5),
            UnitPriceOriginal = reader.GetDecimal(6),
            PurchaseDate = DateOnly.FromDateTime(reader.GetDateTime(7)),
            CreatedAt = reader.GetDateTime(8)
        };
    }
}
