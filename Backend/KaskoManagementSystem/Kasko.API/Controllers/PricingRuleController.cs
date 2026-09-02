using Kasko.Business.DTOs.PricingRule;
using Kasko.Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kasko.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PricingRuleController : ControllerBase
{
    private readonly IPricingRuleService _pricingRuleService;

    public PricingRuleController(
        IPricingRuleService pricingRuleService)
    {
        _pricingRuleService = pricingRuleService;
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> GetAll()
    {
        var rules = await _pricingRuleService.GetAllAsync();

        return Ok(rules);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var rule = await _pricingRuleService.GetByIdAsync(id);

        if (rule == null)
        {
            return NotFound();
        }

        return Ok(rule);
    }
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(
    [FromBody] CreatePricingRuleDto dto)
    {
        var result =
            await _pricingRuleService.CreateAsync(dto);

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Id },
            result);
    }
    [HttpPut]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(
    [FromBody] UpdatePricingRuleDto dto)
    {
        await _pricingRuleService.UpdateAsync(dto);

        return NoContent();
    }
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _pricingRuleService.DeleteAsync(id);

        return NoContent();
    }
}