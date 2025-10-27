using HotWind.Api.Data.Repositories;
using HotWind.Api.Models.Domain;
using HotWind.Api.Models.Requests;
using HotWind.Api.Services;
using Moq;
using Xunit;

namespace HotWind.Api.Tests.Services;

public class ExchangeRateServiceTests
{
    private readonly Mock<IExchangeRateRepository> _mockRepo;
    private readonly ExchangeRateService _service;

    public ExchangeRateServiceTests()
    {
        _mockRepo = new Mock<IExchangeRateRepository>();
        _service = new ExchangeRateService(_mockRepo.Object);
    }

    [Fact]
    public async Task GenerateRatesAsync_ThrowsException_WhenStartDateAfterEndDate()
    {
        // Arrange
        var request = new GenerateRatesRequest
        {
            StartDate = new DateOnly(2024, 12, 31),
            EndDate = new DateOnly(2024, 1, 1)
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.GenerateRatesAsync(request));
    }

    [Fact]
    public async Task GenerateRatesAsync_GeneratesRatesForAllCurrencies()
    {
        // Arrange
        var request = new GenerateRatesRequest
        {
            StartDate = new DateOnly(2024, 1, 1),
            EndDate = new DateOnly(2024, 1, 3)
        };

        var currencies = new List<string> { "USD", "EUR" };

        _mockRepo.Setup(x => x.GetAvailableCurrenciesAsync())
            .ReturnsAsync(currencies);

        _mockRepo.Setup(x => x.RateExistsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateOnly>()))
            .ReturnsAsync(false);

        _mockRepo.Setup(x => x.GetRateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateOnly>()))
            .ThrowsAsync(new InvalidOperationException("No previous rate"));

        var insertedRates = new List<ExchangeRate>();
        _mockRepo.Setup(x => x.InsertRatesAsync(It.IsAny<List<ExchangeRate>>()))
            .ReturnsAsync((List<ExchangeRate> rates) =>
            {
                insertedRates.AddRange(rates);
                return rates.Count;
            });

        // Act
        var count = await _service.GenerateRatesAsync(request);

        // Assert
        // 2 currencies * 3 days = 6 rates
        Assert.Equal(6, insertedRates.Count);

        // Verify all rates are for USD and EUR
        Assert.All(insertedRates, rate =>
        {
            Assert.True(rate.FromCurrency == "USD" || rate.FromCurrency == "EUR");
            Assert.Equal("UAH", rate.ToCurrency);
            Assert.True(rate.Rate > 0);
        });
    }

    [Fact]
    public async Task GenerateRatesAsync_SkipsExistingRates()
    {
        // Arrange
        var request = new GenerateRatesRequest
        {
            StartDate = new DateOnly(2024, 1, 1),
            EndDate = new DateOnly(2024, 1, 2)
        };

        var currencies = new List<string> { "USD" };

        _mockRepo.Setup(x => x.GetAvailableCurrenciesAsync())
            .ReturnsAsync(currencies);

        // First date exists, second doesn't
        _mockRepo.Setup(x => x.RateExistsAsync("USD", "UAH", new DateOnly(2024, 1, 1)))
            .ReturnsAsync(true);

        _mockRepo.Setup(x => x.RateExistsAsync("USD", "UAH", new DateOnly(2024, 1, 2)))
            .ReturnsAsync(false);

        _mockRepo.Setup(x => x.GetRateAsync("USD", "UAH", new DateOnly(2024, 1, 1)))
            .ReturnsAsync(38.50m);

        _mockRepo.Setup(x => x.GetRateAsync("USD", "UAH", new DateOnly(2023, 12, 31)))
            .ThrowsAsync(new InvalidOperationException("No previous rate"));

        var insertedRates = new List<ExchangeRate>();
        _mockRepo.Setup(x => x.InsertRatesAsync(It.IsAny<List<ExchangeRate>>()))
            .ReturnsAsync((List<ExchangeRate> rates) =>
            {
                insertedRates.AddRange(rates);
                return rates.Count;
            });

        // Act
        await _service.GenerateRatesAsync(request);

        // Assert
        // Only 1 rate should be generated (for Jan 2, as Jan 1 exists)
        Assert.Single(insertedRates);
        Assert.Equal(new DateOnly(2024, 1, 2), insertedRates[0].RateDate);
    }

    [Fact]
    public async Task GetRateAsync_ReturnsSameCurrency_WhenFromAndToAreEqual()
    {
        // Act
        var rate = await _service.GetRateAsync("UAH", "UAH", DateOnly.FromDateTime(DateTime.Now));

        // Assert
        Assert.Equal(1.0m, rate);
    }

    [Fact]
    public async Task GetRateAsync_DelegatesToRepository_ForDifferentCurrencies()
    {
        // Arrange
        var date = new DateOnly(2024, 1, 15);
        _mockRepo.Setup(x => x.GetRateAsync("USD", "UAH", date))
            .ReturnsAsync(38.75m);

        // Act
        var rate = await _service.GetRateAsync("USD", "UAH", date);

        // Assert
        Assert.Equal(38.75m, rate);
        _mockRepo.Verify(x => x.GetRateAsync("USD", "UAH", date), Times.Once);
    }
}
