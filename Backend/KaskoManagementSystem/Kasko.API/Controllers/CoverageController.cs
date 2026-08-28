using Kasko.Business.DTOs.Coverage;
using Kasko.Business.Services.Abstract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kasko.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CoverageController : ControllerBase
{
    private readonly ICoverageService _coverageService;

    public CoverageController(
        ICoverageService coverageService)
    {
        _coverageService = coverageService;
    }


    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result =
            await _coverageService.GetAllAsync();

        return Ok(result);
    }


    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(
        Guid id)
    {
        var result =
            await _coverageService.GetByIdAsync(id);

        if (result == null)
        {
            return NotFound();
        }

        return Ok(result);
    }


    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateCoverageDto dto)
    {
        var result =
            await _coverageService.CreateAsync(dto);

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Id },
            result);
    }


    [HttpPut]
    public async Task<IActionResult> Update(
        [FromBody] UpdateCoverageDto dto)
    {
        await _coverageService.UpdateAsync(dto);

        return NoContent();
    }


    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(
        Guid id)
    {
        await _coverageService.DeleteAsync(id);

        return NoContent();
    }
}