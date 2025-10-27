using HotWind.Api.Models.Domain;

namespace HotWind.Api.Data.Repositories;

public interface IExchangeRateRepository
{
    Task<decimal> GetRateAsync(string fromCurrency, string toCurrency, DateOnly date);
    Task<bool> RateExistsAsync(string fromCurrency, string toCurrency, DateOnly date);
    Task<int> InsertRatesAsync(List<ExchangeRate> rates);
    Task<List<string>> GetAvailableCurrenciesAsync();
}
