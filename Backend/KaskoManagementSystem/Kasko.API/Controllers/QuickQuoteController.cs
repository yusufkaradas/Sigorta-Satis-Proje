using Kasko.Business.DTOs.Payment;
using Kasko.Business.DTOs.Policy;
using Kasko.Business.DTOs.QuickQuote;
using Kasko.Business.DTOs.Quote;
using Kasko.Business.Integrations.Insurer;
using Kasko.Business.Interfaces;
using Kasko.Business.Services;
using Kasko.Business.Services.Abstract;
using Kasko.DataAccess.Repositories.Abstract;
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

    private readonly IQuickQuoteVerificationService _verificationService;

    public QuickQuoteController(
        ICustomerService customerService,
        IQuoteService quoteService,
        IVehicleService vehicleService,
        IInsurancePackageService insurancePackageService,
        InsurerQuoteComparisonService comparisonService,
        IPolicyService policyService,
        IPaymentService paymentService,
        IPolicyPdfService policyPdfService,
        IQuickQuoteVerificationService verificationService)
    {
        _verificationService =
            verificationService;

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

    [HttpPost("estimate/pdf")]
    public async Task<IActionResult> EstimatePdf(
        [FromBody] QuickQuoteEstimatePdfRequestDto dto,
        [FromServices] IQuickQuoteEstimateService estimateService,
        CancellationToken cancellationToken)
    {
        var result =
            await estimateService.EstimateAsync(
                dto,
                cancellationToken);

        var bytes =
            await _policyPdfService.GenerateEstimateAsync(result, dto);

        return File(bytes, "application/pdf", $"{(string.IsNullOrWhiteSpace(dto.Reference) ? $"Kasko-Teklif-{DateTime.Now:yyyyMMdd}" : dto.Reference)}.pdf");
    }

    private string? ResolveVerificationToken()
    {
        return Request.Headers["X-QuickQuote-Token"].FirstOrDefault();
    }

    private void EnsureCaller(string? token, string identityNumber, string phoneNumber)
    {
        if (User.Identity?.IsAuthenticated == true &&
            (User.IsInRole("Customer") || User.IsInRole("Admin")))
        {
            return;
        }

        _verificationService.EnsureVerified(token, identityNumber, phoneNumber);
    }

    [HttpPost("purchase")]
    public async Task<IActionResult> Purchase(
        [FromBody] QuickQuotePurchaseRequestDto dto,
        [FromServices] IUnitOfWork unitOfWork)
    {
        if (string.IsNullOrWhiteSpace(dto.IdentityNumber) ||
            string.IsNullOrWhiteSpace(dto.PhoneNumber) ||
            dto.QuoteId == Guid.Empty)
        {
            return BadRequest(new
            {
                message = "Teklif ve müşteri bilgileri zorunludur."
            });
        }

        if (!dto.AcceptedTerms)
        {
            return BadRequest(new
            {
                message = "Satın almak için bilgilendirme metnini onaylamanız gerekiyor."
            });
        }

        EnsureCaller(
            ResolveVerificationToken(),
            dto.IdentityNumber,
            dto.PhoneNumber);

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

        var quote =
            await _quoteService.GetByIdAsync(dto.QuoteId);

        if (quote == null ||
            quote.CustomerId != customer.CustomerId.Value)
        {
            return NotFound(new
            {
                message = "Teklif bulunamadı."
            });
        }

        if (quote.Status != QuoteStatus.Offered && quote.Status != QuoteStatus.Accepted)
        {
            return BadRequest(new
            {
                message = "Teklifiniz yetkili onayında. Onaylandığında satın alabilirsiniz."
            });
        }

        PolicyDto? policy = null;

        PaymentDto? payment = null;

        await unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            if (quote.Status != QuoteStatus.Accepted)
            {
                await _quoteService.ChangeStatusAsync(dto.QuoteId, QuoteStatus.Accepted);
            }

            policy =
                await _policyService.CreateAsync(
                    new PolicyCreateDto
                    {
                        CustomerId = customer.CustomerId.Value,
                        VehicleId = quote.VehicleId,
                        QuoteId = dto.QuoteId,
                        StartDate = DateTime.UtcNow,
                        EndDate = DateTime.UtcNow.AddYears(1)
                    });

            payment =
                await _paymentService.CreateAsync(
                    new PaymentCreateDto
                    {
                        PolicyId = policy.Id,
                        SimulateFailure = dto.SimulateFailure
                    });
        });

        var activePolicy =
            await _policyService.GetByIdAsync(policy!.Id);

        return Ok(new
        {
            policy = activePolicy ?? policy,
            payment
        });
    }

    [HttpPost("otp/send")]
    public async Task<IActionResult> SendOtp(
        [FromBody] QuickQuoteCustomerLookupRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.IdentityNumber) ||
            string.IsNullOrWhiteSpace(dto.PhoneNumber))
        {
            return BadRequest(new
            {
                message = "T.C. Kimlik No ve telefon numarası zorunludur."
            });
        }

        await _customerService.GetForQuickQuoteAsync(
            dto.IdentityNumber,
            dto.PhoneNumber);

        var code =
            _verificationService.SendCode(
                dto.IdentityNumber,
                dto.PhoneNumber);

        return Ok(new
        {
            message = "Doğrulama kodu telefonunuza gönderildi.",
            demoCode = code
        });
    }

    [HttpPost("otp/verify")]
    public IActionResult VerifyOtp(
        [FromBody] QuickQuoteOtpVerifyRequestDto dto)
    {
        var token =
            _verificationService.VerifyCode(
                dto.IdentityNumber,
                dto.PhoneNumber,
                dto.Code);

        return Ok(new
        {
            verificationToken = token
        });
    }

    [HttpPost("customer/lookup")]
    public async Task<IActionResult> CustomerLookup(
        [FromBody]
        QuickQuoteCustomerLookupRequestDto dto)
    {
        EnsureCaller(
            ResolveVerificationToken(),
            dto.IdentityNumber,
            dto.PhoneNumber);

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
        EnsureCaller(
            ResolveVerificationToken(),
            dto.IdentityNumber,
            dto.PhoneNumber);

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
        EnsureCaller(
            ResolveVerificationToken(),
            dto.IdentityNumber,
            dto.PhoneNumber);

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

                CoverageOptionIds =
                    dto.CoverageOptionIds,

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
    [HttpPost("create")]
    public async Task<IActionResult> Create(
    [FromBody]
    QuickQuotePricingRequestDto dto)
    {
        EnsureCaller(
            ResolveVerificationToken(),
            dto.IdentityNumber,
            dto.PhoneNumber);

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

                CoverageOptionIds =
                    dto.CoverageOptionIds,

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

        var offeredQuote =
            await _quoteService
                .GetByIdAsync(quote.Id);

        return Ok(offeredQuote ?? quote);
    }
    [HttpPost("policy/pdf")]
    public async Task<IActionResult> GeneratePolicyPdf(
    [FromBody] QuickQuotePolicyPdfRequestDto dto)
    {
        EnsureCaller(
            ResolveVerificationToken(),
            dto.IdentityNumber,
            dto.PhoneNumber);

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
}