using Kasko.Business.DTOs.PricingRuleChangeRequest;
using Kasko.Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Kasko.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PricingRuleChangeRequestController : ControllerBase
{
    private readonly IPricingRuleChangeRequestService _service;

    public PricingRuleChangeRequestController(
        IPricingRuleChangeRequestService service)
    {
        _service = service;
    }

    [HttpPost]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> Create(
        [FromBody] CreatePricingRuleChangeRequestDto dto)
    {
        var userIdClaim =
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdClaim, out var requestedBy))
        {
            return Unauthorized();
        }

        var result =
            await _service.CreateAsync(dto, requestedBy);

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Id },
            result);
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> GetAll()
    {
        var result = await _service.GetAllAsync();

        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _service.GetByIdAsync(id);

        if (result == null)
        {
            return NotFound();
        }

        return Ok(result);
    }

    [HttpPost("{id:guid}/approve")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Approve(Guid id)
    {
        var userIdClaim =
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdClaim, out var approvedBy))
        {
            return Unauthorized();
        }

        await _service.ApproveAsync(id, approvedBy);

        return NoContent();
    }

    [HttpPost("{id:guid}/reject")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Reject(Guid id)
    {
        var userIdClaim =
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdClaim, out var approvedBy))
        {
            return Unauthorized();
        }

        await _service.RejectAsync(id, approvedBy);

        return NoContent();
    }
}