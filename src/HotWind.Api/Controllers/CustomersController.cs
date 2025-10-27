using HotWind.Api.Data.Repositories;
using HotWind.Api.Models;
using HotWind.Api.Models.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace HotWind.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CustomersController : ControllerBase
{
    private readonly ICustomerRepository _customerRepository;
    private readonly ILogger<CustomersController> _logger;

    public CustomersController(ICustomerRepository customerRepository, ILogger<CustomersController> logger)
    {
        _customerRepository = customerRepository;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<CustomerDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<CustomerDto>>>> GetCustomers(
        [FromQuery] string? search = null,
        [FromQuery] int limit = 100)
    {
        try
        {
            var customers = !string.IsNullOrWhiteSpace(search)
                ? await _customerRepository.SearchAsync(search, limit)
                : await _customerRepository.GetAllAsync(limit);

            var dtos = customers.Select(c => new CustomerDto
            {
                CustomerId = c.CustomerId,
                CompanyName = c.CompanyName,
                ContactPerson = c.ContactPerson,
                Email = c.Email,
                Phone = c.Phone
            }).ToList();

            return Ok(ApiResponse<List<CustomerDto>>.Ok(dtos));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving customers");
            return StatusCode(500, ApiResponse<List<CustomerDto>>.Fail("An unexpected error occurred"));
        }
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<CustomerDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<CustomerDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<CustomerDto>>> GetCustomer(int id)
    {
        try
        {
            var customer = await _customerRepository.GetByIdAsync(id);
            if (customer == null)
            {
                return NotFound(ApiResponse<CustomerDto>.Fail($"Customer {id} not found"));
            }

            var dto = new CustomerDto
            {
                CustomerId = customer.CustomerId,
                CompanyName = customer.CompanyName,
                ContactPerson = customer.ContactPerson,
                Email = customer.Email,
                Phone = customer.Phone
            };

            return Ok(ApiResponse<CustomerDto>.Ok(dto));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving customer {CustomerId}", id);
            return StatusCode(500, ApiResponse<CustomerDto>.Fail("An unexpected error occurred"));
        }
    }
}
