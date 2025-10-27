using HotWind.Api.Data.Repositories;
using HotWind.Api.Models.Domain;
using HotWind.Api.Models.Requests;

namespace HotWind.Api.Services;

public class ExchangeRateService : IExchangeRateService
{
    private readonly IExchangeRateRepository _exchangeRateRepository;
    private static readonly Random _random = new();

    // Geometric Brownian Motion parameters
    private const double Mu = 0.0001;  // Drift (slight upward bias)
    private const double Sigma = 0.015;  // Volatility (1.5% daily)

    public ExchangeRateService(IExchangeRateRepository exchangeRateRepository)
    {
        _exchangeRateRepository = exchangeRateRepository;
    }

    public async Task<int> GenerateRatesAsync(GenerateRatesRequest request)
    {
        if (request.StartDate > request.EndDate)
        {
            throw new ArgumentException("Start date must be before or equal to end date");
        }

        var currencies = await _exchangeRateRepository.GetAvailableCurrenciesAsync();
        var ratesToInsert = new List<ExchangeRate>();

        foreach (var currency in currencies)
        {
            var rates = await GenerateRatesForCurrencyPairAsync(currency, "UAH", request.StartDate, request.EndDate);
            ratesToInsert.AddRange(rates);
        }

        return await _exchangeRateRepository.InsertRatesAsync(ratesToInsert);
    }

    public async Task<decimal> GetRateAsync(string fromCurrency, string toCurrency, DateOnly date)
    {
        return await _exchangeRateRepository.GetRateAsync(fromCurrency, toCurrency, date);
    }

    private async Task<List<ExchangeRate>> GenerateRatesForCurrencyPairAsync(
        string fromCurrency, string toCurrency, DateOnly startDate, DateOnly endDate)
    {
        var rates = new List<ExchangeRate>();

        // Find the last known rate before startDate
        decimal previousRate;
        try
        {
            previousRate = await _exchangeRateRepository.GetRateAsync(fromCurrency, toCurrency, startDate.AddDays(-1));
        }
        catch
        {
            // No previous rate found, generate a random starting rate
            previousRate = GenerateRandomInitialRate(fromCurrency);
        }

        var currentDate = startDate;

        while (currentDate <= endDate)
        {
            // Check if rate already exists
            bool exists = await _exchangeRateRepository.RateExistsAsync(fromCurrency, toCurrency, currentDate);

            if (!exists)
            {
                // Generate new rate using Geometric Brownian Motion
                decimal newRate = GenerateNextRate(previousRate);

                rates.Add(new ExchangeRate
                {
                    FromCurrency = fromCurrency,
                    ToCurrency = toCurrency,
                    RateDate = currentDate,
                    Rate = newRate
                });

                previousRate = newRate;
            }
            else
            {
                // Rate exists, use it as previous for next iteration
                previousRate = await _exchangeRateRepository.GetRateAsync(fromCurrency, toCurrency, currentDate);
            }

            currentDate = currentDate.AddDays(1);
        }

        return rates;
    }

    private static decimal GenerateNextRate(decimal previousRate)
    {
        // Geometric Brownian Motion: S(t+1) = S(t) * exp((μ - σ²/2) * Δt + σ * √Δt * Z)
        // where Z is a standard normal random variable
        const double dt = 1.0;  // One day

        double z = GenerateStandardNormal();
        double drift = (Mu - 0.5 * Sigma * Sigma) * dt;
        double diffusion = Sigma * Math.Sqrt(dt) * z;
        double exponent = drift + diffusion;

        decimal multiplier = (decimal)Math.Exp(exponent);
        decimal newRate = previousRate * multiplier;

        // Round to 6 decimal places
        return Math.Round(newRate, 6);
    }

    private static decimal GenerateRandomInitialRate(string currency)
    {
        // Generate plausible initial exchange rates for different currencies to UAH
        return currency switch
        {
            "USD" => (decimal)(_random.NextDouble() * 5 + 36),  // 36-41 UAH/USD
            "EUR" => (decimal)(_random.NextDouble() * 5 + 40),  // 40-45 UAH/EUR
            "CNY" => (decimal)(_random.NextDouble() * 2 + 4.5), // 4.5-6.5 UAH/CNY
            "PLN" => (decimal)(_random.NextDouble() * 2 + 9),   // 9-11 UAH/PLN
            _ => (decimal)(_random.NextDouble() * 10 + 10)      // Generic 10-20 UAH
        };
    }

    private static double GenerateStandardNormal()
    {
        // Box-Muller transform to generate standard normal random variable
        double u1 = 1.0 - _random.NextDouble(); // Uniform(0,1] random
        double u2 = 1.0 - _random.NextDouble();
        return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
    }
}
