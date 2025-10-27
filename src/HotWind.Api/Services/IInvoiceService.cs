using HotWind.Api.Models.Dtos;
using HotWind.Api.Models.Requests;

namespace HotWind.Api.Services;

public interface IInvoiceService
{
    Task<InvoiceDto> CreateInvoiceAsync(CreateInvoiceRequest request);
    Task<InvoiceDto?> GetInvoiceByIdAsync(int invoiceId);
    Task<List<InvoiceDto>> GetRecentInvoicesAsync(int limit = 50);
}
