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
    public async Task<IActionResult> Reject(Guid id, [FromBody] RejectPricingRuleChangeRequestDto? dto)
    {
        var userIdClaim =
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdClaim, out var approvedBy))
        {
            return Unauthorized();
        }

        await _service.RejectAsync(id, approvedBy, dto?.Reason);

        return NoContent();
    }

    [HttpGet("{id:guid}/impact")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Impact(
        Guid id,
        [FromServices] Kasko.DataAccess.Repositories.Abstract.IUnitOfWork unitOfWork,
        [FromServices] Kasko.DataAccess.Repositories.Abstract.IGenericRepository<Kasko.Entities.Concrete.QuotePricingSnapshot> snapshots)
    {
        var request = await unitOfWork.PricingRuleChangeRequests.GetByIdAsync(id);

        if (request == null)
        {
            return NotFound();
        }

        var rule = await unitOfWork.PricingRules.GetByIdAsync(request.PricingRuleId);

        if (rule == null || request.OldValue == 0)
        {
            return Ok(new { supported = false });
        }

        var code = rule.Code.ToUpperInvariant();

        Func<Kasko.Entities.Concrete.QuotePricingSnapshot, decimal>? factor = code switch
        {
            "BASE_KASKO_RATE" => x => x.BaseRate,
            _ when code.StartsWith("AGE_") => x => x.AgeFactor,
            _ when code.StartsWith("USAGE_") => x => x.UsageFactor,
            _ when code.StartsWith("CLAIMS_") => x => x.ClaimsFactor,
            _ when code.StartsWith("DRIVER_") => x => x.DriverFactor,
            _ when code.StartsWith("REGION_") => x => x.RegionFactor,
            _ => null
        };

        if (factor == null)
        {
            return Ok(new { supported = false });
        }

        var since = DateTime.UtcNow.AddDays(-30);

        var recent = (await snapshots.FindAsync(x => x.CreatedDate >= since && !x.IsDeleted)).ToList();

        var affected = recent.Where(x => factor(x) == request.OldValue).ToList();

        if (affected.Count == 0)
        {
            return Ok(new { supported = true, totalQuotes = recent.Count, affectedQuotes = 0 });
        }

        var ratio = request.NewValue / request.OldValue;

        var oldAverage = affected.Average(x => x.FinalPremium);

        var newAverage = affected.Average(x => (x.FinalPremium - x.CoveragePremium) * ratio + x.CoveragePremium);

        return Ok(new
        {
            supported = true,
            totalQuotes = recent.Count,
            affectedQuotes = affected.Count,
            oldAverage = Math.Round(oldAverage, 2),
            newAverage = Math.Round(newAverage, 2),
            changePercent = oldAverage == 0 ? 0 : Math.Round((newAverage - oldAverage) / oldAverage * 100, 1)
        });
    }
}
