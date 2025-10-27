namespace HotWind.Api.Models.Domain;

public class PurchaseLot
{
    public int LotId { get; set; }
    public int PoId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string LotNumber { get; set; } = string.Empty;
    public int QuantityPurchased { get; set; }
    public int QuantityRemaining { get; set; }
    public decimal UnitPriceOriginal { get; set; }
    public DateOnly PurchaseDate { get; set; }
    public DateTime CreatedAt { get; set; }
}
