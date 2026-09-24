using System.Security.Claims;
using Kasko.Business.Integrations.VehicleValue;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kasko.Business.Interfaces;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class VehicleValueCatalogController : ControllerBase
{
    private readonly IVehicleValueCatalogService _catalogService;

    private readonly CatalogImportTracker _importTracker;

    private const string ImportBusyMessage =
        "Devam eden bir TSB liste yüklemesi var. Tamamlandıktan sonra yeni liste yükleyebilirsiniz.";

    public VehicleValueCatalogController(
        IVehicleValueCatalogService catalogService,
        CatalogImportTracker importTracker)
    {
        _catalogService = catalogService;

        _importTracker = importTracker;
    }

    [HttpGet("lookup")]
    [AllowAnonymous]
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
    [AllowAnonymous]
    public async Task<IActionResult> GetTypes(
    [FromQuery] string brandCode,
    [FromQuery] string? category,
    [FromQuery] int? modelYear,
    CancellationToken cancellationToken)
    {
        var result =
            await _catalogService.GetTypesAsync(
                brandCode,
                category,
                modelYear,
                cancellationToken);

        return Ok(result);
    }
    [HttpGet("years")]
    [AllowAnonymous]
    public async Task<IActionResult> GetYears(
    [FromQuery] string brandCode,
    [FromQuery] string? typeCode,
    [FromQuery] string? category,
    CancellationToken cancellationToken)
    {
        var result =
            await _catalogService.GetYearsAsync(
                brandCode,
                typeCode,
                category,
                cancellationToken);

        return Ok(result);
    }
    [HttpGet("brands")]
    [AllowAnonymous]
    public async Task<IActionResult> GetBrands(
    [FromQuery] string? category,
    CancellationToken cancellationToken)
    {
        var result =
            await _catalogService.GetBrandsAsync(
                category,
                cancellationToken);

        return Ok(result);
    }

    [HttpGet("categories")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCategories(
        CancellationToken cancellationToken)
    {
        var result =
            await _catalogService.GetCategoriesAsync(cancellationToken);

        return Ok(result);
    }

    [HttpGet("summary")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Summary()
    {
        return Ok(await _catalogService.GetSummaryAsync());
    }

    [HttpPost("reclassify")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Reclassify(
        CancellationToken cancellationToken)
    {
        if (_importTracker.IsBusy)
        {
            return Conflict(ImportBusyMessage);
        }

        var updated =
            await _catalogService.ReclassifyAsync(cancellationToken);

        return Ok(new { updated });
    }

    [HttpGet("import/status")]
    [Authorize(Roles = "Admin")]
    public IActionResult ImportStatus()
    {
        return Ok(_importTracker.GetStatus());
    }

    [HttpPost("import")]
    [Authorize(Roles = "Admin")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(30_000_000)]
    public async Task<IActionResult> Import(
        IFormFile file,
        [FromForm] DateTime? effectiveDate,
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

        if (_importTracker.IsBusy)
        {
            return Conflict(ImportBusyMessage);
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
        }
        catch
        {
            DeleteTempFile(tempFilePath);
            throw;
        }

        var job = new CatalogImportJob
        {
            FilePath = tempFilePath,
            FileName = Path.GetFileName(file.FileName),
            EffectiveDate = effectiveDate?.Date,
            RequestedByUserId = Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) ? userId : null,
            RequestedBy = User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
        };

        if (!_importTracker.TryEnqueue(job))
        {
            DeleteTempFile(tempFilePath);

            return Conflict(ImportBusyMessage);
        }

        return Accepted(_importTracker.GetStatus());
    }

    private static void DeleteTempFile(string path)
    {
        if (System.IO.File.Exists(path))
        {
            System.IO.File.Delete(path);
        }
    }
}