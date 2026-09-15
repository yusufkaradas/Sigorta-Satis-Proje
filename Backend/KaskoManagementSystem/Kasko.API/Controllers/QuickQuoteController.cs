using Kasko.Business.DTOs.Payment;
using Kasko.Business.DTOs.Policy;
using Kasko.Business.DTOs.QuickQuote;
using Kasko.Business.DTOs.Quote;
using Kasko.Business.Integrations.Insurer;
using Kasko.Business.Interfaces;
using Kasko.Business.Services;
using Kasko.Business.Services.Abstract;
using Kasko.Entities.Enums;
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
    private readonly IPolicyService _policyService;
    private readonly IPaymentService _paymentService;
    private readonly IPolicyPdfService _policyPdfService;

    public QuickQuoteController(
        ICustomerService customerService,
        IQuoteService quoteService,
        IVehicleService vehicleService,
        IInsurancePackageService insurancePackageService,
        InsurerQuoteComparisonService comparisonService,
        IPolicyService policyService,
        IPaymentService paymentService,
        IPolicyPdfService policyPdfService)
    {
        _customerService =
            customerService;

        _quoteService =
            quoteService;

        _vehicleService =
            vehicleService;
       
        _insurancePackageService =
            insurancePackageService;
        
        _comparisonService = 
            comparisonService;
        
        _policyService = 
            policyService;

        _paymentService = 
            paymentService;
        _policyPdfService = 
            policyPdfService;
    }

    [HttpPost("estimate")]
    public async Task<IActionResult> Estimate(
        [FromBody] QuickQuoteEstimateRequestDto dto,
        [FromServices] IQuickQuoteEstimateService estimateService,
        CancellationToken cancellationToken)
    {
        var result =
            await estimateService.EstimateAsync(
                dto,
                cancellationToken);

        return Ok(result);
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
    [HttpPost("create")]
    public async Task<IActionResult> Create(
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

        var quote =
            await _quoteService
                .CreateAsync(createQuoteDto);

        return Ok(quote);
    }
    [HttpPost("offer")]
    public async Task<IActionResult> Offer(
    [FromBody] QuickQuoteOfferRequestDto dto)
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

        if (dto.QuoteId == Guid.Empty)
        {
            return BadRequest(new
            {
                message =
                    "Teklif bilgisi zorunludur."
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

        var quote =
            await _quoteService
                .GetByIdAsync(dto.QuoteId);

        if (quote == null)
        {
            return NotFound(new
            {
                message =
                    "Teklif bulunamadı."
            });
        }

        if (
            quote.CustomerId !=
            customer.CustomerId.Value)
        {
            return NotFound(new
            {
                message =
                    "Teklif bulunamadı."
            });
        }

        await _quoteService
            .ChangeStatusAsync(
                dto.QuoteId,
                QuoteStatus.Offered);

        return NoContent();
    }
    [HttpPost("accept")]
    public async Task<IActionResult> Accept(
    [FromBody] QuickQuoteOfferRequestDto dto)
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

        if (dto.QuoteId == Guid.Empty)
        {
            return BadRequest(new
            {
                message =
                    "Teklif bilgisi zorunludur."
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

        var quote =
            await _quoteService
                .GetByIdAsync(dto.QuoteId);

        if (quote == null ||
            quote.CustomerId != customer.CustomerId.Value)
        {
            return NotFound(new
            {
                message =
                    "Teklif bulunamadı."
            });
        }

        await _quoteService
            .ChangeStatusAsync(
                dto.QuoteId,
                QuoteStatus.Accepted);

        return NoContent();
    }
    [HttpPost("policy/create")]
    public async Task<IActionResult> CreatePolicy(
    [FromBody] QuickQuotePolicyCreateRequestDto dto)
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

        if (dto.QuoteId == Guid.Empty)
        {
            return BadRequest(new
            {
                message =
                    "Teklif bilgisi zorunludur."
            });
        }

        var customer =
            await _customerService.GetForQuickQuoteAsync(
                dto.IdentityNumber,
                dto.PhoneNumber);

        if (!customer.Found || !customer.CustomerId.HasValue)
        {
            return NotFound(new
            {
                message = "Müşteri doğrulanamadı."
            });
        }

        var policy =
            await _policyService.CreateAsync(
                new PolicyCreateDto
                {
                    CustomerId = customer.CustomerId.Value,
                    VehicleId = dto.VehicleId,
                    QuoteId = dto.QuoteId,
                    StartDate = DateTime.UtcNow,
                    EndDate = DateTime.UtcNow.AddYears(1)
                });

        return Ok(policy);
    }
    [HttpPost("payment/create")]
    public async Task<IActionResult> CreatePayment(
    [FromBody] QuickQuotePaymentRequestDto dto)
    {
        if (
            string.IsNullOrWhiteSpace(dto.IdentityNumber) ||
            string.IsNullOrWhiteSpace(dto.PhoneNumber))
        {
            return BadRequest(new
            {
                message = "Müşteri bilgileri zorunludur."
            });
        }

        if (dto.PolicyId == Guid.Empty)
        {
            return BadRequest(new
            {
                message = "Poliçe bilgisi zorunludur."
            });
        }

        var customer =
            await _customerService.GetForQuickQuoteAsync(
                dto.IdentityNumber,
                dto.PhoneNumber);

        if (!customer.Found || !customer.CustomerId.HasValue)
        {
            return NotFound(new
            {
                message = "Müşteri doğrulanamadı."
            });
        }

        var policy =
            await _policyService.GetByIdAsync(dto.PolicyId);

        if (
            policy == null ||
            policy.CustomerId != customer.CustomerId.Value)
        {
            return NotFound(new
            {
                message = "Poliçe bulunamadı."
            });
        }

        var payment =
            await _paymentService.CreateAsync(
                new PaymentCreateDto
                {
                    PolicyId = dto.PolicyId,
                    SimulateFailure = dto.SimulateFailure
                });

        return Ok(payment);
    }
    [HttpPost("policy/pdf")]
    public async Task<IActionResult> GeneratePolicyPdf(
    [FromBody] QuickQuotePolicyPdfRequestDto dto)
    {
        if (dto.PolicyId == Guid.Empty)
        {
            return BadRequest(new
            {
                message = "Poliçe bilgisi zorunludur."
            });
        }

        var customer =
            await _customerService.GetForQuickQuoteAsync(
                dto.IdentityNumber,
                dto.PhoneNumber);

        if (!customer.Found || !customer.CustomerId.HasValue)
        {
            return NotFound(new
            {
                message = "Müşteri doğrulanamadı."
            });
        }

        var policy =
            await _policyService.GetByIdAsync(dto.PolicyId);

        if (
            policy == null ||
            policy.CustomerId != customer.CustomerId.Value ||
            policy.Status != PolicyStatus.Active)
        {
            return NotFound(new
            {
                message = "Poliçe bulunamadı."
            });
        }

        var pdf =
            await _policyPdfService
                .GenerateAsync(dto.PolicyId);

        return File(
            pdf,
            "application/pdf",
            $"{policy.PolicyNumber}.pdf");
    }
    [HttpPost("customer/create")]
    public async Task<IActionResult> CreateCustomer(
    [FromBody]
    QuickQuoteCustomerCreateRequestDto dto)
    {
        if (
            string.IsNullOrWhiteSpace(dto.IdentityNumber) ||
            dto.IdentityNumber.Length != 11)
        {
            return BadRequest(new
            {
                message =
                    "T.C. Kimlik No 11 haneli olmalıdır."
            });
        }

        if (string.IsNullOrWhiteSpace(dto.PhoneNumber))
        {
            return BadRequest(new
            {
                message =
                    "Telefon numarası zorunludur."
            });
        }

        var customerId =
            await _customerService
                .CreateForQuickQuoteAsync(dto);

        return Ok(new
        {
            customerId
        });
    }
}