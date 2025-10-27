using HotWind.Cli.Models;

namespace HotWind.Cli.Services;

public interface IApiClient
{
    Task<List<StockReportItem>> GetStockReportAsync();
    Task<List<PriceListReportItem>> GetPriceListReportAsync();
    Task<List<CurrencyTranslationReportItem>> GetCurrencyTranslationReportAsync(DateOnly startDate, DateOnly endDate);
    Task<List<HeaterModel>> GetModelsAsync(string? search = null, bool inStockOnly = false);
    Task<List<Customer>> GetCustomersAsync(string? search = null);
    Task<int> CreateInvoiceAsync(CreateInvoiceRequest request);
    Task<int> GenerateExchangeRatesAsync(GenerateRatesRequest request);
}
