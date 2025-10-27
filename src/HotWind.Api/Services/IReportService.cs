using HotWind.Api.Models.Dtos;

namespace HotWind.Api.Services;

public interface IReportService
{
    Task<List<StockReportItemDto>> GetStockReportAsync();
    Task<List<PriceListReportItemDto>> GetPriceListReportAsync();
    Task<List<CurrencyTranslationReportItemDto>> GetCurrencyTranslationReportAsync(DateOnly startDate, DateOnly endDate);
}
