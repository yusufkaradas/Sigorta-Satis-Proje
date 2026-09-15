using Kasko.Business.DTOs.QuickQuote;
using Kasko.Business.Exceptions;
using Kasko.Business.Interfaces;
using Kasko.Business.Pricing;

namespace Kasko.Business.Services;

public class QuickQuoteEstimateService : IQuickQuoteEstimateService
{
    private static readonly string[] AllowedUsages = { "PRIVATE", "COMMERCIAL" };

    private readonly IVehicleValueCatalogService _vehicleValueCatalogService;
    private readonly IInsurancePackageService _insurancePackageService;
    private readonly IPricingService _pricingService;

    public QuickQuoteEstimateService(
        IVehicleValueCatalogService vehicleValueCatalogService,
        IInsurancePackageService insurancePackageService,
        IPricingService pricingService)
    {
        _vehicleValueCatalogService = vehicleValueCatalogService;
        _insurancePackageService = insurancePackageService;
        _pricingService = pricingService;
    }

    public async Task<QuickQuoteEstimateResultDto> EstimateAsync(
        QuickQuoteEstimateRequestDto request,
        CancellationToken cancellationToken = default)
    {
        Validate(request);

        var vehicleValue =
            await _vehicleValueCatalogService.LookupAsync(
                request.BrandCode.Trim(),
                request.TypeCode.Trim(),
                request.ModelYear,
                cancellationToken);

        if (vehicleValue == null || vehicleValue.Value <= 0)
        {
            throw new NotFoundException(
                "Seçilen araç için TSB kasko değeri bulunamadı.");
        }

        var packages =
            (await _insurancePackageService.GetAllAsync())
                .Where(x => x.IsActive)
                .Where(x => !request.PackageId.HasValue || x.Id == request.PackageId.Value)
                .OrderBy(x => x.Factor)
                .ToList();

        if (packages.Count == 0)
        {
            throw new NotFoundException(
                "Teklif hesaplanacak aktif kasko paketi bulunamadı.");
        }

        var driverAge =
            DateTime.UtcNow.Year - request.BirthYear;

        var result = new QuickQuoteEstimateResultDto
        {
            BrandName = vehicleValue.BrandName,
            TypeName = vehicleValue.TypeName,
            ModelYear = vehicleValue.ModelYear,
            MarketValue = vehicleValue.Value
        };

        foreach (var package in packages)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var calculation =
                await _pricingService.CalculateAsync(
                    new PricingRequest
                    {
                        MarketValue = vehicleValue.Value,
                        ModelYear = vehicleValue.ModelYear,
                        DriverAge = driverAge,
                        Usage = request.Usage.Trim().ToUpperInvariant(),
                        ClaimsCount = request.ClaimsCount,
                        Region = "NORMAL",
                        PackageId = package.Id,
                        Deductible = request.Deductible,
                        CoverageIds = package.Coverages
                            .Where(x => x.IsDefault)
                            .Select(x => x.CoverageId)
                            .ToArray(),
                        EffectiveDate = DateTime.UtcNow
                    },
                    cancellationToken);

            result.Packages.Add(new QuickQuotePackageEstimateDto
            {
                PackageId = package.Id,
                PackageCode = package.Code,
                PackageName = package.Name,
                Description = package.Description,
                TotalPremium = calculation.TotalPremium,
                CoveragePremium = calculation.CoveragePremium,
                Discount = calculation.Discount,
                Coverages = calculation.Coverages
                    .Select(x => new QuickQuoteCoverageEstimateDto
                    {
                        CoverageId = x.CoverageId,
                        CoverageName = x.CoverageName,
                        Price = x.CalculatedPrice
                    })
                    .ToList()
            });
        }

        return result;
    }

    private static void Validate(QuickQuoteEstimateRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.BrandCode) ||
            string.IsNullOrWhiteSpace(request.TypeCode))
        {
            throw new BadRequestException("Marka ve model seçimi zorunludur.");
        }

        var currentYear = DateTime.UtcNow.Year;

        if (request.ModelYear < 1950 || request.ModelYear > currentYear + 1)
        {
            throw new BadRequestException("Geçerli bir model yılı seçin.");
        }

        var age = currentYear - request.BirthYear;

        if (age < 18 || age > 100)
        {
            throw new BadRequestException("Sürücü yaşı 18 ile 100 arasında olmalıdır.");
        }

        if (!AllowedUsages.Contains(request.Usage?.Trim().ToUpperInvariant()))
        {
            throw new BadRequestException("Kullanım tarzı geçersiz.");
        }

        if (request.ClaimsCount < 0 || request.ClaimsCount > 10)
        {
            throw new BadRequestException("Hasar sayısı 0 ile 10 arasında olmalıdır.");
        }

        if (request.Deductible < 0)
        {
            throw new BadRequestException("Muafiyet tutarı negatif olamaz.");
        }
    }
}
