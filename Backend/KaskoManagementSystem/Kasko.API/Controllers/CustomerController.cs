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
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> GetAll()
    {
        var customers = await _customerService.GetAllAsync();

        return Ok(customers);
    }
    [HttpGet("me")]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> GetCurrent()
    {
        var customer = await _customerService.GetCurrentAsync();

        if (customer == null)
            return NotFound();

        return Ok(customer);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var customer = await _customerService.GetByIdAsync(id);

        if (customer == null)
            return NotFound();

        return Ok(customer);
    }

    [HttpPost("{id:guid}/account")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateAccount(
        Guid id,
        [FromServices] ICustomerAccountService accountService)
    {
        var account = await accountService.CreateAccountAsync(id);

        return Ok(new
        {
            email = account.Email,
            temporaryPassword = account.TemporaryPassword
        });
    }

    [HttpPut]
    [Authorize(Roles = "Admin,Customer")]
    public async Task<IActionResult> Update(
        UpdateCustomerDto dto,
        [FromServices] ICustomerAccountService accountService)
    {
        await _customerService.UpdateAsync(dto);

        await accountService.SyncUserAsync(dto.Id);

        return NoContent();
    }

   
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(
        Guid id,
        [FromServices] ICustomerAccountService accountService)
    {
        await _customerService.DeleteAsync(id);

        await accountService.DeactivateUserAsync(id);

        return NoContent();
    }
}