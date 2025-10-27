namespace HotWind.Api.Models.Domain;

public class PurchaseOrder
{
    public int PoId { get; set; }
    public int VendorId { get; set; }
    public DateOnly PoDate { get; set; }
    public decimal? TotalAmount { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
}
