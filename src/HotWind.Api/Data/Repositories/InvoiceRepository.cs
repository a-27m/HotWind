using HotWind.Api.Models.Domain;
using Npgsql;

namespace HotWind.Api.Data.Repositories;

public class InvoiceRepository : IInvoiceRepository
{
    private readonly NpgsqlDataSource _dataSource;

    public InvoiceRepository(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task<int> CreateAsync(Invoice invoice)
    {
        const string insertInvoiceSql = @"
            INSERT INTO invoices (customer_id, invoice_date, total_amount, notes)
            VALUES ($1, $2, $3, $4)
            RETURNING invoice_id";

        const string insertLineSql = @"
            INSERT INTO invoice_lines (invoice_id, sku, quantity_sold, unit_price_uah)
            VALUES ($1, $2, $3, $4)
            RETURNING invoice_line_id";

        await using var conn = await _dataSource.OpenConnectionAsync();
        await using var transaction = await conn.BeginTransactionAsync();

        try
        {
            // Insert invoice
            int invoiceId;
            await using (var cmd = new NpgsqlCommand(insertInvoiceSql, conn, transaction))
            {
                cmd.Parameters.AddWithValue(invoice.CustomerId);
                cmd.Parameters.AddWithValue(invoice.InvoiceDate);
                cmd.Parameters.AddWithValue(invoice.TotalAmount);
                cmd.Parameters.AddWithValue(invoice.Notes ?? (object)DBNull.Value);

                var result = await cmd.ExecuteScalarAsync();
                invoiceId = Convert.ToInt32(result);
            }

            // Insert invoice lines
            foreach (var line in invoice.Lines)
            {
                await using var cmd = new NpgsqlCommand(insertLineSql, conn, transaction);
                cmd.Parameters.AddWithValue(invoiceId);
                cmd.Parameters.AddWithValue(line.Sku);
                cmd.Parameters.AddWithValue(line.QuantitySold);
                cmd.Parameters.AddWithValue(line.UnitPriceUah);

                await cmd.ExecuteNonQueryAsync();
            }

            await transaction.CommitAsync();
            return invoiceId;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<Invoice?> GetByIdAsync(int invoiceId)
    {
        const string invoiceSql = @"
            SELECT invoice_id, customer_id, invoice_date, total_amount, notes, created_at
            FROM invoices
            WHERE invoice_id = $1";

        const string linesSql = @"
            SELECT invoice_line_id, invoice_id, sku, quantity_sold, unit_price_uah, created_at
            FROM invoice_lines
            WHERE invoice_id = $1
            ORDER BY invoice_line_id";

        await using var conn = await _dataSource.OpenConnectionAsync();

        Invoice? invoice = null;

        // Get invoice
        await using (var cmd = new NpgsqlCommand(invoiceSql, conn))
        {
            cmd.Parameters.AddWithValue(invoiceId);

            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                invoice = MapInvoiceFromReader(reader);
            }
        }

        if (invoice == null)
        {
            return null;
        }

        // Get lines
        await using (var cmd = new NpgsqlCommand(linesSql, conn))
        {
            cmd.Parameters.AddWithValue(invoiceId);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                invoice.Lines.Add(MapLineFromReader(reader));
            }
        }

        return invoice;
    }

    public async Task<List<Invoice>> GetRecentAsync(int limit = 50)
    {
        const string sql = @"
            SELECT invoice_id, customer_id, invoice_date, total_amount, notes, created_at
            FROM invoices
            ORDER BY invoice_date DESC, invoice_id DESC
            LIMIT $1";

        var invoices = new List<Invoice>();

        await using var conn = await _dataSource.OpenConnectionAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue(limit);

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            invoices.Add(MapInvoiceFromReader(reader));
        }

        return invoices;
    }

    private static Invoice MapInvoiceFromReader(NpgsqlDataReader reader)
    {
        return new Invoice
        {
            InvoiceId = reader.GetInt32(0),
            CustomerId = reader.GetInt32(1),
            InvoiceDate = DateOnly.FromDateTime(reader.GetDateTime(2)),
            TotalAmount = reader.GetDecimal(3),
            Notes = reader.IsDBNull(4) ? null : reader.GetString(4),
            CreatedAt = reader.GetDateTime(5)
        };
    }

    private static InvoiceLine MapLineFromReader(NpgsqlDataReader reader)
    {
        return new InvoiceLine
        {
            InvoiceLineId = reader.GetInt32(0),
            InvoiceId = reader.GetInt32(1),
            Sku = reader.GetString(2),
            QuantitySold = reader.GetInt32(3),
            UnitPriceUah = reader.GetDecimal(4),
            CreatedAt = reader.GetDateTime(5)
        };
    }
}
