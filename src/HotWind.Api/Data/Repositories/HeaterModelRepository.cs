using HotWind.Api.Models.Domain;
using Npgsql;

namespace HotWind.Api.Data.Repositories;

public class HeaterModelRepository : IHeaterModelRepository
{
    private readonly NpgsqlDataSource _dataSource;

    public HeaterModelRepository(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task<HeaterModel?> GetBySkuAsync(string sku)
    {
        const string sql = @"
            SELECT sku, model_name, manufacturer, capacity_kw, description, created_at
            FROM heater_models
            WHERE sku = $1";

        await using var conn = await _dataSource.OpenConnectionAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue(sku);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return MapFromReader(reader);
        }

        return null;
    }

    public async Task<List<HeaterModel>> GetAllAsync(int limit = 100)
    {
        const string sql = @"
            SELECT sku, model_name, manufacturer, capacity_kw, description, created_at
            FROM heater_models
            ORDER BY model_name
            LIMIT $1";

        var models = new List<HeaterModel>();

        await using var conn = await _dataSource.OpenConnectionAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue(limit);

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            models.Add(MapFromReader(reader));
        }

        return models;
    }

    public async Task<List<HeaterModel>> SearchAsync(string searchTerm, int limit = 20)
    {
        const string sql = @"
            SELECT sku, model_name, manufacturer, capacity_kw, description, created_at
            FROM heater_models
            WHERE model_name ILIKE $1 OR manufacturer ILIKE $1 OR sku ILIKE $1
            ORDER BY model_name
            LIMIT $2";

        var models = new List<HeaterModel>();
        var searchPattern = $"%{searchTerm}%";

        await using var conn = await _dataSource.OpenConnectionAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue(searchPattern);
        cmd.Parameters.AddWithValue(limit);

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            models.Add(MapFromReader(reader));
        }

        return models;
    }

    public async Task<List<HeaterModel>> GetModelsInStockAsync()
    {
        const string sql = @"
            SELECT DISTINCT h.sku, h.model_name, h.manufacturer, h.capacity_kw, h.description, h.created_at
            FROM heater_models h
            INNER JOIN purchase_lots pl ON h.sku = pl.sku
            WHERE pl.quantity_remaining > 0
            ORDER BY h.model_name";

        var models = new List<HeaterModel>();

        await using var conn = await _dataSource.OpenConnectionAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            models.Add(MapFromReader(reader));
        }

        return models;
    }

    private static HeaterModel MapFromReader(NpgsqlDataReader reader)
    {
        return new HeaterModel
        {
            Sku = reader.GetString(0),
            ModelName = reader.GetString(1),
            Manufacturer = reader.GetString(2),
            CapacityKw = reader.IsDBNull(3) ? null : reader.GetDecimal(3),
            Description = reader.IsDBNull(4) ? null : reader.GetString(4),
            CreatedAt = reader.GetDateTime(5)
        };
    }
}
