using HotWind.Api.Data.Repositories;
using HotWind.Api.Models.Domain;
using HotWind.Api.Models.Requests;
using HotWind.Api.Services;
using Moq;
using Xunit;

namespace HotWind.Api.Tests.Services;

public class InvoiceServiceTests
{
    private readonly Mock<IInvoiceRepository> _mockInvoiceRepo;
    private readonly Mock<ICustomerRepository> _mockCustomerRepo;
    private readonly Mock<IHeaterModelRepository> _mockModelRepo;
    private readonly Mock<IPurchaseLotRepository> _mockLotRepo;
    private readonly InvoiceService _service;

    public InvoiceServiceTests()
    {
        _mockInvoiceRepo = new Mock<IInvoiceRepository>();
        _mockCustomerRepo = new Mock<ICustomerRepository>();
        _mockModelRepo = new Mock<IHeaterModelRepository>();
        _mockLotRepo = new Mock<IPurchaseLotRepository>();

        _service = new InvoiceService(
            _mockInvoiceRepo.Object,
            _mockCustomerRepo.Object,
            _mockModelRepo.Object,
            _mockLotRepo.Object);
    }

    [Fact]
    public async Task CreateInvoiceAsync_ThrowsException_WhenCustomerNotFound()
    {
        // Arrange
        var request = new CreateInvoiceRequest
        {
            CustomerId = 999,
            InvoiceDate = DateOnly.FromDateTime(DateTime.Now),
            Lines = new List<CreateInvoiceLineRequest>
            {
                new() { Sku = "TEST-001", Quantity = 1, UnitPrice = 100 }
            }
        };

        _mockCustomerRepo.Setup(x => x.GetByIdAsync(999))
            .ReturnsAsync((Customer?)null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateInvoiceAsync(request));
    }

    [Fact]
    public async Task CreateInvoiceAsync_ThrowsException_WhenModelNotFound()
    {
        // Arrange
        var request = new CreateInvoiceRequest
        {
            CustomerId = 1,
            InvoiceDate = DateOnly.FromDateTime(DateTime.Now),
            Lines = new List<CreateInvoiceLineRequest>
            {
                new() { Sku = "INVALID-SKU", Quantity = 1, UnitPrice = 100 }
            }
        };

        _mockCustomerRepo.Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(new Customer { CustomerId = 1, CompanyName = "Test Customer" });

        _mockModelRepo.Setup(x => x.GetBySkuAsync("INVALID-SKU"))
            .ReturnsAsync((HeaterModel?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateInvoiceAsync(request));

        Assert.Contains("not found", exception.Message);
    }

    [Fact]
    public async Task CreateInvoiceAsync_ThrowsException_WhenInsufficientStock()
    {
        // Arrange
        var request = new CreateInvoiceRequest
        {
            CustomerId = 1,
            InvoiceDate = DateOnly.FromDateTime(DateTime.Now),
            Lines = new List<CreateInvoiceLineRequest>
            {
                new() { Sku = "TEST-001", Quantity = 100, UnitPrice = 100 }
            }
        };

        _mockCustomerRepo.Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(new Customer { CustomerId = 1, CompanyName = "Test Customer" });

        _mockModelRepo.Setup(x => x.GetBySkuAsync("TEST-001"))
            .ReturnsAsync(new HeaterModel { Sku = "TEST-001", ModelName = "Test Model" });

        _mockLotRepo.Setup(x => x.GetTotalStockBySkuAsync("TEST-001"))
            .ReturnsAsync(10); // Only 10 in stock

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateInvoiceAsync(request));

        Assert.Contains("Insufficient stock", exception.Message);
    }

    [Fact]
    public async Task CreateInvoiceAsync_CalculatesTotalCorrectly()
    {
        // Arrange
        var request = new CreateInvoiceRequest
        {
            CustomerId = 1,
            InvoiceDate = DateOnly.FromDateTime(DateTime.Now),
            Lines = new List<CreateInvoiceLineRequest>
            {
                new() { Sku = "TEST-001", Quantity = 2, UnitPrice = 100.50m },
                new() { Sku = "TEST-002", Quantity = 3, UnitPrice = 200.00m }
            }
        };

        _mockCustomerRepo.Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(new Customer { CustomerId = 1, CompanyName = "Test Customer" });

        _mockModelRepo.Setup(x => x.GetBySkuAsync(It.IsAny<string>()))
            .ReturnsAsync((string sku) => new HeaterModel { Sku = sku, ModelName = $"Model {sku}" });

        _mockLotRepo.Setup(x => x.GetTotalStockBySkuAsync(It.IsAny<string>()))
            .ReturnsAsync(100);

        _mockLotRepo.Setup(x => x.GetAvailableLotsBySkuAsync(It.IsAny<string>()))
            .ReturnsAsync((string sku) => new List<PurchaseLot>
            {
                new()
                {
                    LotId = 1,
                    Sku = sku,
                    QuantityRemaining = 100,
                    UnitPriceOriginal = 50,
                    PurchaseDate = DateOnly.FromDateTime(DateTime.Now)
                }
            });

        _mockInvoiceRepo.Setup(x => x.CreateAsync(It.IsAny<Invoice>()))
            .ReturnsAsync(1)
            .Callback<Invoice>(invoice =>
            {
                // Verify the total is calculated correctly: (2 * 100.50) + (3 * 200) = 201 + 600 = 801
                Assert.Equal(801.00m, invoice.TotalAmount);
            });

        _mockInvoiceRepo.Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(new Invoice
            {
                InvoiceId = 1,
                CustomerId = 1,
                InvoiceDate = request.InvoiceDate,
                TotalAmount = 801.00m,
                Lines = new List<InvoiceLine>
                {
                    new() { Sku = "TEST-001", QuantitySold = 2, UnitPriceUah = 100.50m },
                    new() { Sku = "TEST-002", QuantitySold = 3, UnitPriceUah = 200.00m }
                }
            });

        // Act
        var result = await _service.CreateInvoiceAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(801.00m, result.TotalAmount);
    }
}
