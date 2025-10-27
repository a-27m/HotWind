namespace HotWind.Api.Models.Dtos;

public class StockReportItemDto
{
    public string Sku { get; set; } = string.Empty;
    public string ModelName { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;
    public int StockLevel { get; set; }
    public int LotCount { get; set; }
    public decimal WeightedAvgPurchasePriceUah { get; set; }
    public decimal ListPriceUah { get; set; }
    public decimal PotentialProfit { get; set; }
    public decimal ProfitMarginPercent { get; set; }
}
