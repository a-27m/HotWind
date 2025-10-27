using System.Net.Http.Json;
using System.Text.Json;
using HotWind.Cli.Models;

namespace HotWind.Cli.Services;

public class ApiClient : IApiClient
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;

    public ApiClient(string baseUrl)
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(baseUrl),
            Timeout = TimeSpan.FromSeconds(30)
        };

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public async Task<List<StockReportItem>> GetStockReportAsync()
    {
        var response = await _httpClient.GetFromJsonAsync<ApiResponse<List<StockReportItem>>>(
            "api/reports/stock", _jsonOptions);

        if (response?.Success == true && response.Data != null)
        {
            return response.Data;
        }

        throw new Exception(response?.Error ?? "Failed to retrieve stock report");
    }

    public async Task<List<PriceListReportItem>> GetPriceListReportAsync()
    {
        var response = await _httpClient.GetFromJsonAsync<ApiResponse<List<PriceListReportItem>>>(
            "api/reports/price-list", _jsonOptions);

        if (response?.Success == true && response.Data != null)
        {
            return response.Data;
        }

        throw new Exception(response?.Error ?? "Failed to retrieve price list report");
    }

    public async Task<List<CurrencyTranslationReportItem>> GetCurrencyTranslationReportAsync(
        DateOnly startDate, DateOnly endDate)
    {
        var url = $"api/reports/currency-translation?from={startDate:yyyy-MM-dd}&to={endDate:yyyy-MM-dd}";
        var response = await _httpClient.GetFromJsonAsync<ApiResponse<List<CurrencyTranslationReportItem>>>(
            url, _jsonOptions);

        if (response?.Success == true && response.Data != null)
        {
            return response.Data;
        }

        throw new Exception(response?.Error ?? "Failed to retrieve currency translation report");
    }

    public async Task<List<HeaterModel>> GetModelsAsync(string? search = null, bool inStockOnly = false)
    {
        var url = "api/models?";
        if (!string.IsNullOrWhiteSpace(search))
        {
            url += $"search={Uri.EscapeDataString(search)}&";
        }
        if (inStockOnly)
        {
            url += "inStockOnly=true&";
        }
        url = url.TrimEnd('&', '?');

        var response = await _httpClient.GetFromJsonAsync<ApiResponse<List<HeaterModel>>>(
            url, _jsonOptions);

        if (response?.Success == true && response.Data != null)
        {
            return response.Data;
        }

        throw new Exception(response?.Error ?? "Failed to retrieve models");
    }

    public async Task<List<Customer>> GetCustomersAsync(string? search = null)
    {
        var url = "api/customers";
        if (!string.IsNullOrWhiteSpace(search))
        {
            url += $"?search={Uri.EscapeDataString(search)}";
        }

        var response = await _httpClient.GetFromJsonAsync<ApiResponse<List<Customer>>>(
            url, _jsonOptions);

        if (response?.Success == true && response.Data != null)
        {
            return response.Data;
        }

        throw new Exception(response?.Error ?? "Failed to retrieve customers");
    }

    public async Task<int> CreateInvoiceAsync(CreateInvoiceRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("api/invoices", request, _jsonOptions);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<dynamic>>(_jsonOptions);

        if (result?.Success == true)
        {
            return 0; // Success indicator
        }

        throw new Exception(result?.Error ?? "Failed to create invoice");
    }

    public async Task<int> GenerateExchangeRatesAsync(GenerateRatesRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("api/exchangerates/generate", request, _jsonOptions);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<int>>(_jsonOptions);

        if (result?.Success == true)
        {
            return result.Data;
        }

        throw new Exception(result?.Error ?? "Failed to generate exchange rates");
    }
}
