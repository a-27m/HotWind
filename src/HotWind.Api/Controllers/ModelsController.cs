using HotWind.Api.Data.Repositories;
using HotWind.Api.Models;
using HotWind.Api.Models.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace HotWind.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ModelsController : ControllerBase
{
    private readonly IHeaterModelRepository _modelRepository;
    private readonly IPurchaseLotRepository _lotRepository;
    private readonly ILogger<ModelsController> _logger;

    public ModelsController(
        IHeaterModelRepository modelRepository,
        IPurchaseLotRepository lotRepository,
        ILogger<ModelsController> logger)
    {
        _modelRepository = modelRepository;
        _lotRepository = lotRepository;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<HeaterModelDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<HeaterModelDto>>>> GetModels(
        [FromQuery] string? search = null,
        [FromQuery] bool inStockOnly = false,
        [FromQuery] int limit = 100)
    {
        try
        {
            var models = !string.IsNullOrWhiteSpace(search)
                ? await _modelRepository.SearchAsync(search, limit)
                : inStockOnly
                    ? await _modelRepository.GetModelsInStockAsync()
                    : await _modelRepository.GetAllAsync(limit);

            var dtos = new List<HeaterModelDto>();
            foreach (var model in models)
            {
                var stockLevel = await _lotRepository.GetTotalStockBySkuAsync(model.Sku);

                dtos.Add(new HeaterModelDto
                {
                    Sku = model.Sku,
                    ModelName = model.ModelName,
                    Manufacturer = model.Manufacturer,
                    CapacityKw = model.CapacityKw,
                    StockLevel = stockLevel
                });
            }

            return Ok(ApiResponse<List<HeaterModelDto>>.Ok(dtos));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving heater models");
            return StatusCode(500, ApiResponse<List<HeaterModelDto>>.Fail("An unexpected error occurred"));
        }
    }

    [HttpGet("{sku}")]
    [ProducesResponseType(typeof(ApiResponse<HeaterModelDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<HeaterModelDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<HeaterModelDto>>> GetModel(string sku)
    {
        try
        {
            var model = await _modelRepository.GetBySkuAsync(sku);
            if (model == null)
            {
                return NotFound(ApiResponse<HeaterModelDto>.Fail($"Model with SKU '{sku}' not found"));
            }

            var stockLevel = await _lotRepository.GetTotalStockBySkuAsync(model.Sku);

            var dto = new HeaterModelDto
            {
                Sku = model.Sku,
                ModelName = model.ModelName,
                Manufacturer = model.Manufacturer,
                CapacityKw = model.CapacityKw,
                StockLevel = stockLevel
            };

            return Ok(ApiResponse<HeaterModelDto>.Ok(dto));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving heater model {Sku}", sku);
            return StatusCode(500, ApiResponse<HeaterModelDto>.Fail("An unexpected error occurred"));
        }
    }
}
