using HotWind.Api.Models.Domain;

namespace HotWind.Api.Data.Repositories;

public interface IHeaterModelRepository
{
    Task<HeaterModel?> GetBySkuAsync(string sku);
    Task<List<HeaterModel>> GetAllAsync(int limit = 100);
    Task<List<HeaterModel>> SearchAsync(string searchTerm, int limit = 20);
    Task<List<HeaterModel>> GetModelsInStockAsync();
}
