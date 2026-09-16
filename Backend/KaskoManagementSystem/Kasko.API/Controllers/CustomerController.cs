using Kasko.Business.DTOs.Customer;
using Kasko.Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kasko.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CustomerController : ControllerBase
{
    private readonly ICustomerService _customerService;

    public CustomerController(ICustomerService customerService)
    {
        _customerService = customerService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var customers = await _customerService.GetAllAsync();

        return Ok(customers);
    }
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var customer = await _customerService.GetByIdAsync(id);

        if (customer == null)
            return NotFound();

        return Ok(customer);
    }

    [HttpPost]
    [Authorize(Roles="Admin")]
    public async Task<IActionResult> Create(
        CreateCustomerDto dto)
    {
        await _customerService.CreateAsync(dto);

        return StatusCode(StatusCodes.Status201Created);
    }

    [HttpPut]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(
        UpdateCustomerDto dto)
    {
        await _customerService.UpdateAsync(dto);

        return NoContent();
    }

   
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _customerService.DeleteAsync(id);

        return NoContent();
    }
}