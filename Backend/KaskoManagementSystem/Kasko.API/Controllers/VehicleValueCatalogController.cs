using Kasko.Business.Integrations.VehicleValue;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kasko.Business.Interfaces;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class VehicleValueCatalogController : ControllerBase
{
    private readonly IVehicleValueImportService _importService;

    private readonly IVehicleValueCatalogService _catalogService;

    public VehicleValueCatalogController(
        IVehicleValueImportService importService,
        IVehicleValueCatalogService catalogService)
    {
        _importService = importService;

        _catalogService = catalogService;
    }

    [HttpGet("lookup")]
    public async Task<IActionResult> Lookup(
        [FromQuery] string brandCode,
        [FromQuery] string typeCode,
        [FromQuery] int modelYear,
        CancellationToken cancellationToken)
    {
        var result =
            await _catalogService.LookupAsync(
                brandCode,
                typeCode,
                modelYear,
                cancellationToken);

        if (result == null)
        {
            return NotFound(
                "Bu marka, tip ve model yılı için TSB kasko değeri bulunamadı.");
        }

        return Ok(result);
    }
    [HttpGet("types")]
    public async Task<IActionResult> GetTypes(
    [FromQuery] string brandCode,
    CancellationToken cancellationToken)
    {
        var result =
            await _catalogService.GetTypesAsync(
                brandCode,
                cancellationToken);

        return Ok(result);
    }
    [HttpGet("years")]
    public async Task<IActionResult> GetYears(
    [FromQuery] string brandCode,
    [FromQuery] string typeCode,
    CancellationToken cancellationToken)
    {
        var result =
            await _catalogService.GetYearsAsync(
                brandCode,
                typeCode,
                cancellationToken);

        return Ok(result);
    }
    [HttpGet("brands")]
    public async Task<IActionResult> GetBrands(
    CancellationToken cancellationToken)
    {
        var result =
            await _catalogService.GetBrandsAsync(
                cancellationToken);

        return Ok(result);
    }
    [HttpPost("import")]
    [Authorize(Roles = "Admin")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Import(
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(
                "Geçerli bir Excel dosyası gönderilmelidir.");
        }

        var extension =
            Path.GetExtension(file.FileName);

        if (!extension.Equals(
                ".xlsx",
                StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(
                "Sadece .xlsx formatındaki Excel dosyaları kabul edilir.");
        }

        var tempFilePath = Path.Combine(
            Path.GetTempPath(),
            $"{Guid.NewGuid()}{extension}");

        try
        {
            await using (var stream =
                         System.IO.File.Create(tempFilePath))
            {
                await file.CopyToAsync(
                    stream,
                    cancellationToken);
            }

            var result =
                await _importService.ImportAsync(
                    tempFilePath,
                    cancellationToken);

            return Ok(result);
        }
        finally
        {
            if (System.IO.File.Exists(tempFilePath))
            {
                System.IO.File.Delete(tempFilePath);
            }
        }
    }
}