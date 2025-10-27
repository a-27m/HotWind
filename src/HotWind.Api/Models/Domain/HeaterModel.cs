namespace HotWind.Api.Models.Domain;

public class HeaterModel
{
    public string Sku { get; set; } = string.Empty;
    public string ModelName { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;
    public decimal? CapacityKw { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
}
