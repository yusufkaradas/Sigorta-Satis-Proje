using Kasko.Business.DTOs.BrandSetting;
using Kasko.Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kasko.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BrandSettingController : ControllerBase
{
    private readonly IBrandSettingService _brandSettingService;

    public BrandSettingController(
        IBrandSettingService brandSettingService)
    {
        _brandSettingService = brandSettingService;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Get(
        CancellationToken cancellationToken)
    {
        var result =
            await _brandSettingService.GetAsync(cancellationToken);

        return Ok(result);
    }

    [HttpPut]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(
        [FromBody] UpdateBrandSettingDto dto,
        CancellationToken cancellationToken)
    {
        var result =
            await _brandSettingService.UpdateAsync(dto, cancellationToken);

        return Ok(result);
    }
}
