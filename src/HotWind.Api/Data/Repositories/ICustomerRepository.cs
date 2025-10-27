using HotWind.Api.Models.Domain;

namespace HotWind.Api.Data.Repositories;

public interface ICustomerRepository
{
    Task<Customer?> GetByIdAsync(int customerId);
    Task<List<Customer>> GetAllAsync(int limit = 100);
    Task<List<Customer>> SearchAsync(string searchTerm, int limit = 20);
}
