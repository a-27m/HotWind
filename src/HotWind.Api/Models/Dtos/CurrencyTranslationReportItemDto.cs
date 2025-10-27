namespace HotWind.Api.Models.Dtos;

public class CurrencyTranslationReportItemDto
{
    public string Sku { get; set; } = string.Empty;
    public string ModelName { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;
    public int TotalUnitsSold { get; set; }
    public decimal HistoricalValueUah { get; set; }
    public decimal CurrentValueUah { get; set; }
    public decimal ValueDifferenceUah { get; set; }
    public decimal ExchangeRateImpactPercent { get; set; }
}
