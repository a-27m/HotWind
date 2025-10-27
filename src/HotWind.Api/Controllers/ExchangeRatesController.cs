using HotWind.Api.Models;
using HotWind.Api.Models.Requests;
using HotWind.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace HotWind.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ExchangeRatesController : ControllerBase
{
    private readonly IExchangeRateService _exchangeRateService;
    private readonly ILogger<ExchangeRatesController> _logger;

    public ExchangeRatesController(IExchangeRateService exchangeRateService, ILogger<ExchangeRatesController> logger)
    {
        _exchangeRateService = exchangeRateService;
        _logger = logger;
    }

    [HttpPost("generate")]
    [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<int>>> GenerateRates([FromBody] GenerateRatesRequest request)
    {
        try
        {
            int count = await _exchangeRateService.GenerateRatesAsync(request);
            return Ok(ApiResponse<int>.Ok(count));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid request for rate generation");
            return BadRequest(ApiResponse<int>.Fail(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating exchange rates");
            return StatusCode(500, ApiResponse<int>.Fail("An unexpected error occurred"));
        }
    }

    [HttpGet("{from}/{to}")]
    [ProducesResponseType(typeof(ApiResponse<decimal>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<decimal>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<decimal>>> GetRate(
        string from,
        string to,
        [FromQuery] DateOnly? date = null)
    {
        try
        {
            var rateDate = date ?? DateOnly.FromDateTime(DateTime.UtcNow);
            var rate = await _exchangeRateService.GetRateAsync(from, to, rateDate);
            return Ok(ApiResponse<decimal>.Ok(rate));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Exchange rate not found");
            return NotFound(ApiResponse<decimal>.Fail(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving exchange rate");
            return StatusCode(500, ApiResponse<decimal>.Fail("An unexpected error occurred"));
        }
    }
}
