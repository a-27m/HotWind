namespace HotWind.Api.Models.Domain;

public class ExchangeRate
{
    public string FromCurrency { get; set; } = string.Empty;
    public string ToCurrency { get; set; } = string.Empty;
    public DateOnly RateDate { get; set; }
    public decimal Rate { get; set; }
    public DateTime CreatedAt { get; set; }
}
