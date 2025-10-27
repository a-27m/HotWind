namespace HotWind.Api.Models;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Error { get; set; }
    public List<string>? ValidationErrors { get; set; }

    public static ApiResponse<T> Ok(T data)
    {
        return new ApiResponse<T>
        {
            Success = true,
            Data = data
        };
    }

    public static ApiResponse<T> Fail(string error, List<string>? validationErrors = null)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Error = error,
            ValidationErrors = validationErrors
        };
    }
}
