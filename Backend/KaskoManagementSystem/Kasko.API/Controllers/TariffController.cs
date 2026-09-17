using Kasko.Business.DTOs.Tariff;
using Kasko.Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kasko.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,Manager")]
public class TariffController : ControllerBase
{
    private readonly ITariffService _service;

    public TariffController(ITariffService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        return Ok(await _service.GetTariffAsync());
    }

    [HttpGet("requests")]
    public async Task<IActionResult> GetRequests()
    {
        return Ok(await _service.GetRequestsAsync());
    }

    [HttpPost("requests")]
    public async Task<IActionResult> CreateRequest([FromBody] CreateTariffChangeRequestDto dto)
    {
        return Ok(await _service.CreateRequestAsync(dto));
    }

    [HttpPost("requests/{id:guid}/approve")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Approve(Guid id, [FromBody] DecideTariffChangeRequestDto dto)
    {
        await _service.ApproveAsync(id, dto?.Note);

        return NoContent();
    }

    [HttpPost("requests/{id:guid}/reject")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Reject(Guid id, [FromBody] DecideTariffChangeRequestDto dto)
    {
        await _service.RejectAsync(id, dto?.Note);

        return NoContent();
    }
}
