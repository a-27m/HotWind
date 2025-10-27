namespace HotWind.Api.Models.Dtos;

public class CustomerDto
{
    public int CustomerId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string? ContactPerson { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
}
