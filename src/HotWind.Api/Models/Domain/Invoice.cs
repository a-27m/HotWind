namespace HotWind.Api.Models.Domain;

public class Invoice
{
    public int InvoiceId { get; set; }
    public int CustomerId { get; set; }
    public DateOnly InvoiceDate { get; set; }
    public decimal TotalAmount { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public List<InvoiceLine> Lines { get; set; } = new();
}
