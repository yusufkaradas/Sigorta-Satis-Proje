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

    public PreviousPolicyController(
        IPreviousPolicyService service)
    {
        _service = service;
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Customer")]
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

        if (result == null)
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
        var result =
            await _service.GetByCustomerIdAsync(
                customerId,
                cancellationToken);

        return Ok(result);
    }
}