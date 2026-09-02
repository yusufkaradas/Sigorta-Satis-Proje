using Kasko.Business.DTOs.Package;
using Kasko.Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kasko.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class InsurancePackageController : ControllerBase
{
    private readonly IInsurancePackageService _service;

    public InsurancePackageController(
        IInsurancePackageService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<InsurancePackageDto>>> GetAll()
    {
        var packages = await _service.GetAllAsync();

        return Ok(packages);
    }
}