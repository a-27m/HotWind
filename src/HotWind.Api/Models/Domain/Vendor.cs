namespace HotWind.Api.Models.Domain;

public class Vendor
{
    public int VendorId { get; set; }
    public string VendorName { get; set; } = string.Empty;
    public string CountryCode { get; set; } = string.Empty;
    public string CurrencyCode { get; set; } = string.Empty;
    public string? ContactInfo { get; set; }
    public DateTime CreatedAt { get; set; }
}
