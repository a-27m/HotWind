using HotWind.Api.Models;
using HotWind.Api.Models.Dtos;
using HotWind.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace HotWind.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(IReportService reportService, ILogger<ReportsController> logger)
    {
        _reportService = reportService;
        _logger = logger;
    }

    [HttpGet("stock")]
    [ProducesResponseType(typeof(ApiResponse<List<StockReportItemDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<StockReportItemDto>>>> GetStockReport()
    {
        try
        {
            var report = await _reportService.GetStockReportAsync();
            return Ok(ApiResponse<List<StockReportItemDto>>.Ok(report));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating stock report");
            return StatusCode(500, ApiResponse<List<StockReportItemDto>>.Fail("An unexpected error occurred"));
        }
    }

    [HttpGet("price-list")]
    [ProducesResponseType(typeof(ApiResponse<List<PriceListReportItemDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<PriceListReportItemDto>>>> GetPriceListReport()
    {
        try
        {
            var report = await _reportService.GetPriceListReportAsync();
            return Ok(ApiResponse<List<PriceListReportItemDto>>.Ok(report));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating price list report");
            return StatusCode(500, ApiResponse<List<PriceListReportItemDto>>.Fail("An unexpected error occurred"));
        }
    }

    [HttpGet("currency-translation")]
    [ProducesResponseType(typeof(ApiResponse<List<CurrencyTranslationReportItemDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<CurrencyTranslationReportItemDto>>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<List<CurrencyTranslationReportItemDto>>>> GetCurrencyTranslationReport(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to)
    {
        if (!from.HasValue || !to.HasValue)
        {
            return BadRequest(ApiResponse<List<CurrencyTranslationReportItemDto>>.Fail(
                "Both 'from' and 'to' date parameters are required"));
        }

        try
        {
            var report = await _reportService.GetCurrencyTranslationReportAsync(from.Value, to.Value);
            return Ok(ApiResponse<List<CurrencyTranslationReportItemDto>>.Ok(report));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid date range for currency translation report");
            return BadRequest(ApiResponse<List<CurrencyTranslationReportItemDto>>.Fail(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating currency translation report");
            return StatusCode(500, ApiResponse<List<CurrencyTranslationReportItemDto>>.Fail("An unexpected error occurred"));
        }
    }
}
