using HotWind.Api.Data.Repositories;
using HotWind.Api.Models.Dtos;
using HotWind.Api.Services;
using Moq;
using Xunit;

namespace HotWind.Api.Tests.Services;

public class ReportServiceTests
{
    private readonly Mock<IReportRepository> _mockRepo;
    private readonly ReportService _service;

    public ReportServiceTests()
    {
        _mockRepo = new Mock<IReportRepository>();
        _service = new ReportService(_mockRepo.Object);
    }

    [Fact]
    public async Task GetStockReportAsync_DelegatesToRepository()
    {
        // Arrange
        var expectedReport = new List<StockReportItemDto>
        {
            new()
            {
                Sku = "TEST-001",
                ModelName = "Test Model",
                Manufacturer = "Test Mfg",
                StockLevel = 100,
                WeightedAvgPurchasePriceUah = 1000m,
                ListPriceUah = 1200m,
                PotentialProfit = 20000m,
                ProfitMarginPercent = 20m
            }
        };

        _mockRepo.Setup(x => x.GetStockReportAsync())
            .ReturnsAsync(expectedReport);

        // Act
        var result = await _service.GetStockReportAsync();

        // Assert
        Assert.Same(expectedReport, result);
        _mockRepo.Verify(x => x.GetStockReportAsync(), Times.Once);
    }

    [Fact]
    public async Task GetPriceListReportAsync_DelegatesToRepository()
    {
        // Arrange
        var expectedReport = new List<PriceListReportItemDto>
        {
            new()
            {
                Sku = "TEST-001",
                ModelName = "Test Model",
                Manufacturer = "Test Mfg",
                StockLevel = 50,
                WeightedLotValueUah = 50000m,
                CurrentMarketValueUah = 60000m,
                ValueDifferenceUah = 10000m,
                ValueDifferencePercent = 20m
            }
        };

        _mockRepo.Setup(x => x.GetPriceListReportAsync())
            .ReturnsAsync(expectedReport);

        // Act
        var result = await _service.GetPriceListReportAsync();

        // Assert
        Assert.Same(expectedReport, result);
        _mockRepo.Verify(x => x.GetPriceListReportAsync(), Times.Once);
    }

    [Fact]
    public async Task GetCurrencyTranslationReportAsync_ThrowsException_WhenStartDateAfterEndDate()
    {
        // Arrange
        var startDate = new DateOnly(2024, 12, 31);
        var endDate = new DateOnly(2024, 1, 1);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.GetCurrencyTranslationReportAsync(startDate, endDate));
    }

    [Fact]
    public async Task GetCurrencyTranslationReportAsync_DelegatesToRepository_WithValidDates()
    {
        // Arrange
        var startDate = new DateOnly(2024, 1, 1);
        var endDate = new DateOnly(2024, 12, 31);

        var expectedReport = new List<CurrencyTranslationReportItemDto>
        {
            new()
            {
                Sku = "TEST-001",
                ModelName = "Test Model",
                Manufacturer = "Test Mfg",
                TotalUnitsSold = 100,
                HistoricalValueUah = 100000m,
                CurrentValueUah = 105000m,
                ValueDifferenceUah = 5000m,
                ExchangeRateImpactPercent = 5m
            }
        };

        _mockRepo.Setup(x => x.GetCurrencyTranslationReportAsync(startDate, endDate))
            .ReturnsAsync(expectedReport);

        // Act
        var result = await _service.GetCurrencyTranslationReportAsync(startDate, endDate);

        // Assert
        Assert.Same(expectedReport, result);
        _mockRepo.Verify(x => x.GetCurrencyTranslationReportAsync(startDate, endDate), Times.Once);
    }
}
