namespace HotWind.Api.Models.Dtos;

public class InvoiceDto
{
    public int InvoiceId { get; set; }
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public DateOnly InvoiceDate { get; set; }
    public decimal TotalAmount { get; set; }
    public string? Notes { get; set; }
    public List<InvoiceLineDto> Lines { get; set; } = new();
}

public class InvoiceLineDto
{
    public int InvoiceLineId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string ModelName { get; set; } = string.Empty;
    public int QuantitySold { get; set; }
    public decimal UnitPriceUah { get; set; }
    public decimal LineTotal { get; set; }
}
