using HotWind.Api.Models.Requests;

namespace HotWind.Api.Services;

public interface IExchangeRateService
{
    Task<int> GenerateRatesAsync(GenerateRatesRequest request);
    Task<decimal> GetRateAsync(string fromCurrency, string toCurrency, DateOnly date);
}
