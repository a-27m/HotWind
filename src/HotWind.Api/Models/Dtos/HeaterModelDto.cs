namespace HotWind.Api.Models.Dtos;

public class HeaterModelDto
{
    public string Sku { get; set; } = string.Empty;
    public string ModelName { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;
    public decimal? CapacityKw { get; set; }
    public int? StockLevel { get; set; }
    public decimal? ListPriceUah { get; set; }
}
