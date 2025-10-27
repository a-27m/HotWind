namespace HotWind.Api.Models.Domain;

public class ListPrice
{
    public int PriceId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public decimal ListPriceUah { get; set; }
    public DateOnly EffectiveDate { get; set; }
    public bool IsCurrent { get; set; }
    public DateTime CreatedAt { get; set; }
}
