using HotWind.Api.Models.Domain;

namespace HotWind.Api.Data.Repositories;

public interface IInvoiceRepository
{
    Task<int> CreateAsync(Invoice invoice);
    Task<Invoice?> GetByIdAsync(int invoiceId);
    Task<List<Invoice>> GetRecentAsync(int limit = 50);
}
