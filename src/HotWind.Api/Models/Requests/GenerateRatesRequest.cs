namespace HotWind.Api.Models.Requests;

public class GenerateRatesRequest
{
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
}
