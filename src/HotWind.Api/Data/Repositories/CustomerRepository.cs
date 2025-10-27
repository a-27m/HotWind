using HotWind.Api.Models.Domain;
using Npgsql;

namespace HotWind.Api.Data.Repositories;

public class CustomerRepository : ICustomerRepository
{
    private readonly NpgsqlDataSource _dataSource;

    public CustomerRepository(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task<Customer?> GetByIdAsync(int customerId)
    {
        const string sql = @"
            SELECT customer_id, company_name, contact_person, email, phone, created_at
            FROM customers
            WHERE customer_id = $1";

        await using var conn = await _dataSource.OpenConnectionAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue(customerId);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return MapFromReader(reader);
        }

        return null;
    }

    public async Task<List<Customer>> GetAllAsync(int limit = 100)
    {
        const string sql = @"
            SELECT customer_id, company_name, contact_person, email, phone, created_at
            FROM customers
            ORDER BY company_name
            LIMIT $1";

        var customers = new List<Customer>();

        await using var conn = await _dataSource.OpenConnectionAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue(limit);

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            customers.Add(MapFromReader(reader));
        }

        return customers;
    }

    public async Task<List<Customer>> SearchAsync(string searchTerm, int limit = 20)
    {
        const string sql = @"
            SELECT customer_id, company_name, contact_person, email, phone, created_at
            FROM customers
            WHERE company_name ILIKE $1 OR contact_person ILIKE $1
            ORDER BY company_name
            LIMIT $2";

        var customers = new List<Customer>();
        var searchPattern = $"%{searchTerm}%";

        await using var conn = await _dataSource.OpenConnectionAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue(searchPattern);
        cmd.Parameters.AddWithValue(limit);

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            customers.Add(MapFromReader(reader));
        }

        return customers;
    }

    private static Customer MapFromReader(NpgsqlDataReader reader)
    {
        return new Customer
        {
            CustomerId = reader.GetInt32(0),
            CompanyName = reader.GetString(1),
            ContactPerson = reader.IsDBNull(2) ? null : reader.GetString(2),
            Email = reader.IsDBNull(3) ? null : reader.GetString(3),
            Phone = reader.IsDBNull(4) ? null : reader.GetString(4),
            CreatedAt = reader.GetDateTime(5)
        };
    }
}
