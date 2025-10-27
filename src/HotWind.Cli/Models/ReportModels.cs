namespace HotWind.Cli.Models;

public class StockReportItem
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

public class PriceListReportItem
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

public class CurrencyTranslationReportItem
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

public class HeaterModel
{
    public string Sku { get; set; } = string.Empty;
    public string ModelName { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;
    public decimal? CapacityKw { get; set; }
    public int? StockLevel { get; set; }
    public decimal? ListPriceUah { get; set; }
}

public class Customer
{
    public int CustomerId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string? ContactPerson { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
}

public class InvoiceLine
{
    public string Sku { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

public class CreateInvoiceRequest
{
    public int CustomerId { get; set; }
    public DateOnly InvoiceDate { get; set; }
    public string? Notes { get; set; }
    public List<InvoiceLine> Lines { get; set; } = new();
}

public class GenerateRatesRequest
{
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
}
