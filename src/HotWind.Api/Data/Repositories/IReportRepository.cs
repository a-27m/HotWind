using HotWind.Api.Models.Dtos;

namespace HotWind.Api.Data.Repositories;

public interface IReportRepository
{
    Task<List<StockReportItemDto>> GetStockReportAsync();
    Task<List<PriceListReportItemDto>> GetPriceListReportAsync();
    Task<List<CurrencyTranslationReportItemDto>> GetCurrencyTranslationReportAsync(DateOnly startDate, DateOnly endDate);
}
