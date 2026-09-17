using Kasko.Business.Interfaces;
using Kasko.Business.DTOs.PreviousPolicy;
using Kasko.Business.Services;
using Kasko.Entities.Concrete;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kasko.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PreviousPolicyController : ControllerBase
{
    private readonly IPreviousPolicyService _service;

    private readonly ICustomerService _customerService;

    public PreviousPolicyController(
        IPreviousPolicyService service,
        ICustomerService customerService)
    {
        _service = service;
        _customerService = customerService;
    }

    private async Task<bool> CanAccessCustomerAsync(Guid customerId)
    {
        if (!User.IsInRole("Customer"))
        {
            return true;
        }

        var current = await _customerService.GetCurrentAsync();

        return current != null && current.Id == customerId;
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(
        [FromBody] CreatePreviousPolicyDto dto,
        CancellationToken cancellationToken)
    {
        var previousPolicy = new PreviousPolicy
        {
            CustomerId = dto.CustomerId,
            PreviousInsurer = dto.PreviousInsurer,
            PolicyNumber = dto.PolicyNumber,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            ClaimsCount = dto.ClaimsCount
        };

        var result =
            await _service.CreateAsync(
                previousPolicy,
                cancellationToken);

        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result =
            await _service.GetByIdAsync(
                id,
                cancellationToken);

        if (result == null ||
            !await CanAccessCustomerAsync(result.CustomerId))
        {
            return NotFound();
        }

        return Ok(result);
    }

    [HttpGet("customer/{customerId:guid}")]
    public async Task<IActionResult> GetByCustomerId(
        Guid customerId,
        CancellationToken cancellationToken)
    {
        if (!await CanAccessCustomerAsync(customerId))
        {
            return NotFound();
        }

        var result =
            await _service.GetByCustomerIdAsync(
                customerId,
                cancellationToken);

        return Ok(result);
    }
}