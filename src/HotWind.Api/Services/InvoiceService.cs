using HotWind.Api.Data.Repositories;
using HotWind.Api.Models.Domain;
using HotWind.Api.Models.Dtos;
using HotWind.Api.Models.Requests;

namespace HotWind.Api.Services;

public class InvoiceService : IInvoiceService
{
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IHeaterModelRepository _modelRepository;
    private readonly IPurchaseLotRepository _lotRepository;

    public InvoiceService(
        IInvoiceRepository invoiceRepository,
        ICustomerRepository customerRepository,
        IHeaterModelRepository modelRepository,
        IPurchaseLotRepository lotRepository)
    {
        _invoiceRepository = invoiceRepository;
        _customerRepository = customerRepository;
        _modelRepository = modelRepository;
        _lotRepository = lotRepository;
    }

    public async Task<InvoiceDto> CreateInvoiceAsync(CreateInvoiceRequest request)
    {
        // Validate customer exists
        var customer = await _customerRepository.GetByIdAsync(request.CustomerId);
        if (customer == null)
        {
            throw new InvalidOperationException($"Customer with ID {request.CustomerId} not found");
        }

        // Validate all SKUs exist and have sufficient stock
        foreach (var line in request.Lines)
        {
            var model = await _modelRepository.GetBySkuAsync(line.Sku);
            if (model == null)
            {
                throw new InvalidOperationException($"Heater model with SKU '{line.Sku}' not found");
            }

            var availableStock = await _lotRepository.GetTotalStockBySkuAsync(line.Sku);
            if (availableStock < line.Quantity)
            {
                throw new InvalidOperationException(
                    $"Insufficient stock for SKU '{line.Sku}'. Available: {availableStock}, Requested: {line.Quantity}");
            }
        }

        // Calculate total
        decimal totalAmount = request.Lines.Sum(l => l.Quantity * l.UnitPrice);

        // Create invoice domain object
        var invoice = new Invoice
        {
            CustomerId = request.CustomerId,
            InvoiceDate = request.InvoiceDate,
            TotalAmount = totalAmount,
            Notes = request.Notes,
            Lines = request.Lines.Select(l => new InvoiceLine
            {
                Sku = l.Sku,
                QuantitySold = l.Quantity,
                UnitPriceUah = l.UnitPrice
            }).ToList()
        };

        // Create invoice in database (this includes lines)
        int invoiceId = await _invoiceRepository.CreateAsync(invoice);

        // Deduct inventory using FIFO
        foreach (var line in request.Lines)
        {
            await DeductInventoryFifoAsync(line.Sku, line.Quantity);
        }

        // Fetch and return the created invoice
        var createdInvoice = await GetInvoiceByIdAsync(invoiceId);
        if (createdInvoice == null)
        {
            throw new InvalidOperationException("Failed to retrieve created invoice");
        }

        return createdInvoice;
    }

    public async Task<InvoiceDto?> GetInvoiceByIdAsync(int invoiceId)
    {
        var invoice = await _invoiceRepository.GetByIdAsync(invoiceId);
        if (invoice == null)
        {
            return null;
        }

        var customer = await _customerRepository.GetByIdAsync(invoice.CustomerId);

        return new InvoiceDto
        {
            InvoiceId = invoice.InvoiceId,
            CustomerId = invoice.CustomerId,
            CustomerName = customer?.CompanyName ?? "Unknown",
            InvoiceDate = invoice.InvoiceDate,
            TotalAmount = invoice.TotalAmount,
            Notes = invoice.Notes,
            Lines = await MapInvoiceLinesToDtosAsync(invoice.Lines)
        };
    }

    public async Task<List<InvoiceDto>> GetRecentInvoicesAsync(int limit = 50)
    {
        var invoices = await _invoiceRepository.GetRecentAsync(limit);
        var dtos = new List<InvoiceDto>();

        foreach (var invoice in invoices)
        {
            var customer = await _customerRepository.GetByIdAsync(invoice.CustomerId);

            dtos.Add(new InvoiceDto
            {
                InvoiceId = invoice.InvoiceId,
                CustomerId = invoice.CustomerId,
                CustomerName = customer?.CompanyName ?? "Unknown",
                InvoiceDate = invoice.InvoiceDate,
                TotalAmount = invoice.TotalAmount,
                Notes = invoice.Notes,
                Lines = await MapInvoiceLinesToDtosAsync(invoice.Lines)
            });
        }

        return dtos;
    }

    private async Task DeductInventoryFifoAsync(string sku, int quantityToDeduct)
    {
        // Get available lots ordered by purchase date (FIFO)
        var availableLots = await _lotRepository.GetAvailableLotsBySkuAsync(sku);

        int remaining = quantityToDeduct;

        foreach (var lot in availableLots)
        {
            if (remaining <= 0)
            {
                break;
            }

            int deductFromThisLot = Math.Min(remaining, lot.QuantityRemaining);
            int newQuantity = lot.QuantityRemaining - deductFromThisLot;

            await _lotRepository.UpdateQuantityRemainingAsync(lot.LotId, newQuantity);

            remaining -= deductFromThisLot;
        }

        if (remaining > 0)
        {
            throw new InvalidOperationException(
                $"Failed to deduct full quantity for SKU '{sku}'. Remaining: {remaining}");
        }
    }

    private async Task<List<InvoiceLineDto>> MapInvoiceLinesToDtosAsync(List<InvoiceLine> lines)
    {
        var dtos = new List<InvoiceLineDto>();

        foreach (var line in lines)
        {
            var model = await _modelRepository.GetBySkuAsync(line.Sku);

            dtos.Add(new InvoiceLineDto
            {
                InvoiceLineId = line.InvoiceLineId,
                Sku = line.Sku,
                ModelName = model?.ModelName ?? "Unknown",
                QuantitySold = line.QuantitySold,
                UnitPriceUah = line.UnitPriceUah,
                LineTotal = line.QuantitySold * line.UnitPriceUah
            });
        }

        return dtos;
    }
}
