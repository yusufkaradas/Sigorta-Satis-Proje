using Kasko.Business.DTOs.QuickQuote;
using Kasko.Business.DTOs.Quote;
using Kasko.Business.Integrations.Insurer;
using Kasko.Business.Interfaces;
using Kasko.Business.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kasko.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class QuickQuoteController : ControllerBase
{
    private readonly ICustomerService _customerService;
    private readonly IQuoteService _quoteService;
    private readonly IVehicleService _vehicleService;
    private readonly IInsurancePackageService _insurancePackageService;
    private readonly InsurerQuoteComparisonService _comparisonService;

    public QuickQuoteController(
        ICustomerService customerService,
        IQuoteService quoteService,
        IVehicleService vehicleService,
        IInsurancePackageService insurancePackageService,
        InsurerQuoteComparisonService comparisonService)
    {
        _customerService =
            customerService;

        _quoteService =
            quoteService;

        _vehicleService =
            vehicleService;
        _insurancePackageService =
            insurancePackageService;
        _comparisonService = comparisonService;
    }

    [HttpPost("customer/lookup")]
    public async Task<IActionResult> CustomerLookup(
        [FromBody]
        QuickQuoteCustomerLookupRequestDto dto)
    {
        if (
            string.IsNullOrWhiteSpace(dto.IdentityNumber) ||
            string.IsNullOrWhiteSpace(dto.PhoneNumber))
        {
            return BadRequest(new
            {
                message =
                    "T.C. Kimlik No / Vergi No ve telefon numarası zorunludur."
            });
        }


        var result =
            await _customerService
                .GetForQuickQuoteAsync(
                    dto.IdentityNumber,
                    dto.PhoneNumber);


        return Ok(result);
    }

    [HttpPost("customer/vehicles")]
    public async Task<IActionResult> CustomerVehicles(
        [FromBody]
        QuickQuoteCustomerLookupRequestDto dto)
    {
        if (
            string.IsNullOrWhiteSpace(dto.IdentityNumber) ||
            string.IsNullOrWhiteSpace(dto.PhoneNumber))
        {
            return BadRequest(new
            {
                message =
                    "T.C. Kimlik No / Vergi No ve telefon numarası zorunludur."
            });
        }


        var customer =
            await _customerService
                .GetForQuickQuoteAsync(
                    dto.IdentityNumber,
                    dto.PhoneNumber);


        if (
            !customer.Found ||
            !customer.CustomerId.HasValue)
        {
            return NotFound(new
            {
                message =
                    "Müşteri bulunamadı."
            });
        }


        var vehicles =
            await _vehicleService
                .GetAllAsync(
                    customer.CustomerId.Value);


        var activeVehicles =
            vehicles
                .Where(x => x.IsActive)
                .ToList();


        return Ok(activeVehicles);
    }

    [HttpPost("calculate")]
    public async Task<IActionResult> Calculate(
        [FromBody]
        QuickQuotePricingRequestDto dto)
    {
        if (
            string.IsNullOrWhiteSpace(dto.IdentityNumber) ||
            string.IsNullOrWhiteSpace(dto.PhoneNumber))
        {
            return BadRequest(new
            {
                message =
                    "T.C. Kimlik No / Vergi No ve telefon numarası zorunludur."
            });
        }


        if (dto.VehicleId == Guid.Empty)
        {
            return BadRequest(new
            {
                message =
                    "Araç seçimi zorunludur."
            });
        }


        var customer =
            await _customerService
                .GetForQuickQuoteAsync(
                    dto.IdentityNumber,
                    dto.PhoneNumber);


        if (
            !customer.Found ||
            !customer.CustomerId.HasValue)
        {
            return NotFound(new
            {
                message =
                    "Müşteri doğrulanamadı."
            });
        }


        var createQuoteDto =
            new CreateQuoteDto
            {
                CustomerId =
                    customer.CustomerId.Value,

                VehicleId =
                    dto.VehicleId,

                ValidUntil =
                    DateTime.UtcNow.AddDays(7),

                EffectiveDate =
                    DateTime.UtcNow,

                CoverageIds =
                    dto.CoverageIds,

                Usage =
                    dto.Usage,

                ClaimsCount =
                    dto.ClaimsCount,

                PackageId =
                    dto.PackageId,

                Deductible =
                    dto.Deductible,

                PreviousPolicyId =
                    null
            };


        var result =
            await _quoteService
                .CalculateAsync(
                    createQuoteDto);


        return Ok(result);
    }
    [HttpGet("packages")]
    public async Task<IActionResult> GetPackages()
    {
        var packages =
            await _insurancePackageService
                .GetAllAsync();

        return Ok(packages);
    }
    [HttpPost("compare")]
    public async Task<IActionResult> Compare(
    [FromBody]
    QuickQuotePricingRequestDto dto)
    {
        if (
            string.IsNullOrWhiteSpace(dto.IdentityNumber) ||
            string.IsNullOrWhiteSpace(dto.PhoneNumber))
        {
            return BadRequest(new
            {
                message =
                    "T.C. Kimlik No / Vergi No ve telefon numarası zorunludur."
            });
        }


        if (dto.VehicleId == Guid.Empty)
        {
            return BadRequest(new
            {
                message =
                    "Araç seçimi zorunludur."
            });
        }


        var customer =
            await _customerService
                .GetForQuickQuoteAsync(
                    dto.IdentityNumber,
                    dto.PhoneNumber);


        if (
            !customer.Found ||
            !customer.CustomerId.HasValue)
        {
            return NotFound(new
            {
                message =
                    "Müşteri doğrulanamadı."
            });
        }


        var vehicle =
            await _vehicleService
                .GetByIdAsync(
                    dto.VehicleId);


        if (vehicle == null)
        {
            return NotFound(new
            {
                message =
                    "Araç bulunamadı."
            });
        }


        if (
            vehicle.CustomerId !=
            customer.CustomerId.Value)
        {
            return BadRequest(new
            {
                message =
                    "Seçilen araç bu müşteriye ait değil."
            });
        }


        var pricingRequest =
            new Kasko.Business.Pricing.PricingRequest
            {
                MarketValue =
                    vehicle.MarketValue,

                ModelYear =
                    vehicle.ModelYear,

                Usage =
                    dto.Usage,

                ClaimsCount =
                    dto.ClaimsCount,

                PackageId =
                    dto.PackageId,

                Deductible =
                    dto.Deductible,

                CoverageIds =
                    dto.CoverageIds,

                EffectiveDate =
                    DateTime.UtcNow
            };


        var results =
            await _comparisonService
                .CompareAsync(
                    pricingRequest);


        return Ok(results);
    }
}