namespace HotWind.Api.Models.Requests;

public class CreateInvoiceRequest
{
    public int CustomerId { get; set; }
    public DateOnly InvoiceDate { get; set; }
    public string? Notes { get; set; }
    public List<CreateInvoiceLineRequest> Lines { get; set; } = new();
}

public class CreateInvoiceLineRequest
{
    public string Sku { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
