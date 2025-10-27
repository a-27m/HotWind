using HotWind.Api.Models.Domain;

namespace HotWind.Api.Data.Repositories;

public interface IPurchaseLotRepository
{
    Task<List<PurchaseLot>> GetAvailableLotsBySkuAsync(string sku);
    Task UpdateQuantityRemainingAsync(int lotId, int newQuantity);
    Task<int> GetTotalStockBySkuAsync(string sku);
}
