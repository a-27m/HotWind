namespace HotWind.Api.Models.Dtos;

public class PriceListReportItemDto
{
    public string Sku { get; set; } = string.Empty;
    public string ModelName { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;
    public int StockLevel { get; set; }
    public decimal WeightedLotValueUah { get; set; }
    public decimal CurrentMarketValueUah { get; set; }
    public decimal ValueDifferenceUah { get; set; }
    public decimal ValueDifferencePercent { get; set; }
}
