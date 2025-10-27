using HotWind.Api.Data.Repositories;
using HotWind.Api.Models.Dtos;

namespace HotWind.Api.Services;

public class ReportService : IReportService
{
    private readonly IReportRepository _reportRepository;

    public ReportService(IReportRepository reportRepository)
    {
        _reportRepository = reportRepository;
    }

    public async Task<List<StockReportItemDto>> GetStockReportAsync()
    {
        return await _reportRepository.GetStockReportAsync();
    }

    public async Task<List<PriceListReportItemDto>> GetPriceListReportAsync()
    {
        return await _reportRepository.GetPriceListReportAsync();
    }

    public async Task<List<CurrencyTranslationReportItemDto>> GetCurrencyTranslationReportAsync(
        DateOnly startDate, DateOnly endDate)
    {
        if (startDate > endDate)
        {
            throw new ArgumentException("Start date must be before or equal to end date");
        }

        return await _reportRepository.GetCurrencyTranslationReportAsync(startDate, endDate);
    }
}
