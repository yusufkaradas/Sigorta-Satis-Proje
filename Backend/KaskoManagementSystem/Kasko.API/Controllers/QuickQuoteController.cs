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
using Kasko.Business.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Kasko.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
[EnableRateLimiting("public")]
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

    private readonly VerificationCodePolicy _verificationCodePolicy;

    public QuickQuoteController(
        ICustomerService customerService,
        IQuoteService quoteService,
        IVehicleService vehicleService,
        IInsurancePackageService insurancePackageService,
        InsurerQuoteComparisonService comparisonService,
        IPolicyService policyService,
        IPaymentService paymentService,
        IPolicyPdfService policyPdfService,
        IQuickQuoteVerificationService verificationService,
        VerificationCodePolicy verificationCodePolicy)
    {
        _verificationService =
            verificationService;

        _verificationCodePolicy =
            verificationCodePolicy;

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

    [HttpGet("plate-eligibility")]
    public async Task<IActionResult> PlateEligibility([FromQuery] string plate)
    {
        var result = await _quoteService.CheckPlateEligibilityAsync(plate);

        if (!result.Eligible)
        {
            result.Message = "Bu plakaya ait süresi devam eden bir kasko poliçesi bulunuyor. Yeni teklif, poliçe bitimine 60 gün kala yenileme olarak alınabilir.";
        }

        return Ok(result);
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

    private async Task EnsureCallerAsync(string? token, string identityNumber, string phoneNumber)
    {
        if (User.Identity?.IsAuthenticated == true && User.IsInRole("Admin"))
        {
            return;
        }

        if (User.Identity?.IsAuthenticated == true && User.IsInRole("Customer"))
        {
            var current = await _customerService.GetCurrentAsync();

            if (current != null &&
                Digits(current.IdentityNumber) == Digits(identityNumber) &&
                NormalizePhone(current.PhoneNumber) == NormalizePhone(phoneNumber))
            {
                return;
            }
        }

        _verificationService.EnsureVerified(token, identityNumber, phoneNumber);
    }

    private static string Digits(string? value)
    {
        return new string((value ?? string.Empty).Where(char.IsDigit).ToArray());
    }

    private static string NormalizePhone(string? value)
    {
        var digits = Digits(value);

        if (digits.Length == 12 && digits.StartsWith("90"))
        {
            digits = digits[2..];
        }

        if (digits.Length == 11 && digits.StartsWith("0"))
        {
            digits = digits[1..];
        }

        return digits;
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

        await EnsureCallerAsync(
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
                        StartDate = quote.PolicyStartDate.HasValue && quote.PolicyStartDate.Value.Date > DateTime.UtcNow.Date
                            ? quote.PolicyStartDate.Value.Date
                            : DateTime.UtcNow,
                        EndDate = (quote.PolicyStartDate.HasValue && quote.PolicyStartDate.Value.Date > DateTime.UtcNow.Date
                            ? quote.PolicyStartDate.Value.Date
                            : DateTime.UtcNow).AddYears(1)
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
    [EnableRateLimiting("auth")]
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
            demoCode = _verificationCodePolicy.Reveal(code)
        });
    }

    [HttpPost("otp/verify")]
    [EnableRateLimiting("auth")]
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
        await EnsureCallerAsync(
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
        await EnsureCallerAsync(
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
        await EnsureCallerAsync(
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
                    null,

                EnforceSingleOpenQuote =
                    true,

                PolicyStartDate =
                    dto.PolicyStartDate
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
        await EnsureCallerAsync(
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
                    null,

                EnforceSingleOpenQuote =
                    true,

                PolicyStartDate =
                    dto.PolicyStartDate
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
        await EnsureCallerAsync(
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

    [HttpPost("quote/pdf")]
    public async Task<IActionResult> GenerateQuotePdf(
    [FromBody] QuickQuoteQuotePdfRequestDto dto)
    {
        await EnsureCallerAsync(
            ResolveVerificationToken(),
            dto.IdentityNumber,
            dto.PhoneNumber);

        if (dto.QuoteId == Guid.Empty)
        {
            return BadRequest(new
            {
                message = "Teklif bilgisi zorunludur."
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

        var quote =
            await _quoteService.GetByIdAsync(dto.QuoteId);

        if (
            quote == null ||
            quote.CustomerId != customer.CustomerId.Value)
        {
            return NotFound(new
            {
                message = "Teklif bulunamadı."
            });
        }

        var pdf =
            await _policyPdfService
                .GenerateQuoteAsync(dto.QuoteId);

        return File(
            pdf,
            "application/pdf",
            $"Teklif-{quote.QuoteNumber}.pdf");
    }
}