using HotWind.Api.Models;
using HotWind.Api.Models.Dtos;
using HotWind.Api.Models.Requests;
using HotWind.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace HotWind.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InvoicesController : ControllerBase
{
    private readonly IInvoiceService _invoiceService;
    private readonly ILogger<InvoicesController> _logger;

    public InvoicesController(IInvoiceService invoiceService, ILogger<InvoicesController> logger)
    {
        _invoiceService = invoiceService;
        _logger = logger;
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<InvoiceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<InvoiceDto>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<InvoiceDto>>> CreateInvoice([FromBody] CreateInvoiceRequest request)
    {
        try
        {
            var invoice = await _invoiceService.CreateInvoiceAsync(request);
            return Ok(ApiResponse<InvoiceDto>.Ok(invoice));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to create invoice: {Message}", ex.Message);
            return BadRequest(ApiResponse<InvoiceDto>.Fail(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error creating invoice");
            return StatusCode(500, ApiResponse<InvoiceDto>.Fail("An unexpected error occurred"));
        }
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<InvoiceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<InvoiceDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<InvoiceDto>>> GetInvoice(int id)
    {
        try
        {
            var invoice = await _invoiceService.GetInvoiceByIdAsync(id);
            if (invoice == null)
            {
                return NotFound(ApiResponse<InvoiceDto>.Fail($"Invoice {id} not found"));
            }

            return Ok(ApiResponse<InvoiceDto>.Ok(invoice));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving invoice {InvoiceId}", id);
            return StatusCode(500, ApiResponse<InvoiceDto>.Fail("An unexpected error occurred"));
        }
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<InvoiceDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<InvoiceDto>>>> GetRecentInvoices([FromQuery] int limit = 50)
    {
        try
        {
            var invoices = await _invoiceService.GetRecentInvoicesAsync(limit);
            return Ok(ApiResponse<List<InvoiceDto>>.Ok(invoices));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving recent invoices");
            return StatusCode(500, ApiResponse<List<InvoiceDto>>.Fail("An unexpected error occurred"));
        }
    }
}
