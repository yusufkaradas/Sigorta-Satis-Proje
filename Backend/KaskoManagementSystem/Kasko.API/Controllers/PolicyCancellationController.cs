using Kasko.Business.DTOs.PolicyCancellation;
using Kasko.Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kasko.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PolicyCancellationController : ControllerBase
{
    private readonly IPolicyCancellationService _service;

    public PolicyCancellationController(IPolicyCancellationService service)
    {
        _service = service;
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Manager,Customer")]
    public async Task<IActionResult> GetAll()
    {
        return Ok(await _service.GetAllAsync());
    }

    [HttpPost]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> Create([FromBody] CreatePolicyCancellationDto dto)
    {
        return Ok(await _service.CreateAsync(dto));
    }

    [HttpPost("{id:guid}/approve")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Approve(Guid id, [FromBody] DecidePolicyCancellationDto dto)
    {
        await _service.ApproveAsync(id, dto?.Note);

        return NoContent();
    }

    [HttpPost("{id:guid}/reject")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Reject(Guid id, [FromBody] DecidePolicyCancellationDto dto)
    {
        await _service.RejectAsync(id, dto?.Note);

        return NoContent();
    }
}
