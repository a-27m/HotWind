namespace HotWind.Api.Models.Domain;

public class InvoiceLine
{
    public int InvoiceLineId { get; set; }
    public int InvoiceId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public int QuantitySold { get; set; }
    public decimal UnitPriceUah { get; set; }
    public DateTime CreatedAt { get; set; }
}
