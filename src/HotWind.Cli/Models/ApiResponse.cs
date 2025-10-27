namespace HotWind.Cli.Models;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Error { get; set; }
    public List<string>? ValidationErrors { get; set; }
}
