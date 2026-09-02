using Kasko.DataAccess.Repositories.Abstract;
using Kasko.Entities.Enums;

namespace Kasko.Business.Pricing;

public class PricingService : IPricingService
{
    private readonly IUnitOfWork _unitOfWork;

    public PricingService(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PricingCalculation> CalculateAsync(
        PricingRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.MarketValue <= 0)
        {
            throw new ArgumentException(
                "Araç değeri 0'dan büyük olmalıdır.",
                nameof(request.MarketValue));
        }

        var currentYear =
            DateTime.UtcNow.Year;

        var vehicleAge =
            currentYear - request.ModelYear;

        if (request.PackageId.HasValue &&
            request.CoverageIds.Count > 0)
        {
            var packageCoverageIds =
                (await _unitOfWork.PackageCoverages.FindAsync(
                    x =>
                        x.InsurancePackageId == request.PackageId.Value &&
                        !x.IsDeleted))
                .Select(x => x.CoverageId)
                .ToHashSet();

            var invalidCoverageIds =
                request.CoverageIds
                    .Where(x => !packageCoverageIds.Contains(x))
                    .ToList();

            if (invalidCoverageIds.Count > 0)
            {
                throw new InvalidOperationException(
                    "Seçilen teminatlardan biri seçilen pakete ait değil.");
            }
        }

        if (vehicleAge < 0)
        {
            vehicleAge = 0;
        }

        var baseRate =
            await GetRuleValueAsync(
                "BASE_KASKO_RATE",
                request.EffectiveDate);

        var ageFactor =
            await GetAgeFactorAsync(
                vehicleAge,
                request.EffectiveDate);

        var usageFactor =
            await GetUsageFactorAsync(
                request.Usage,
                request.EffectiveDate);

        var driverFactor =
            await GetDriverFactorAsync(
                request.DriverAge,
                request.EffectiveDate);

        var claimsFactor =
            await GetClaimsFactorAsync(
                request.ClaimsCount,
                request.EffectiveDate);

        var regionFactor =
            await GetRegionFactorAsync(
                request.Region,
                request.EffectiveDate);

        var packageFactor =
            await GetPackageFactorAsync(
                request.PackageId);

        var deductibleFactor =
            await GetDeductibleFactorAsync(
                request.Deductible);

        var basePremium =
            request.MarketValue * baseRate;

        var riskAdjustedPremium =
            basePremium *
            ageFactor *
            usageFactor *
            claimsFactor *
            driverFactor *
            regionFactor *
            packageFactor *
            deductibleFactor;

        var coverageResults =
            await CalculateCoveragesAsync(
                request.MarketValue,
                request.CoverageIds);

        var coveragePremium =
            coverageResults.Sum(
                x => x.CalculatedPrice);

        var totalPremium =
            riskAdjustedPremium +
            coveragePremium;

        return new PricingCalculation
        {
            MarketValue = request.MarketValue,

            BaseRate = baseRate,

            AgeFactor = ageFactor,

            UsageFactor = usageFactor,

            DriverFactor = driverFactor,

            ClaimsFactor = claimsFactor,

            RegionFactor = regionFactor,

            PackageFactor = packageFactor,

            DeductibleFactor = deductibleFactor,

            BasePremium =
                decimal.Round(
                    basePremium,
                    2),

            RiskAdjustedPremium =
                decimal.Round(
                    riskAdjustedPremium,
                    2),

            Coverages =
                coverageResults,

            CoveragePremium =
                decimal.Round(
                    coveragePremium,
                    2),

            TotalPremium =
                decimal.Round(
                    totalPremium,
                    2)
        };
    }

    private async Task<IReadOnlyList<PricingCoverageResult>>
        CalculateCoveragesAsync(
            decimal marketValue,
            IReadOnlyCollection<Guid> coverageIds)
    {
        if (coverageIds.Count == 0)
        {
            return Array.Empty<PricingCoverageResult>();
        }

        var distinctCoverageIds =
            coverageIds
                .Distinct()
                .ToArray();

        var coverages =
            await _unitOfWork
                .Coverages
                .FindAsync(x =>
                    distinctCoverageIds.Contains(x.Id) &&
                    x.IsActive);

        var coverageList =
            coverages.ToList();

        if (coverageList.Count != distinctCoverageIds.Length)
        {
            throw new InvalidOperationException(
                "Seçilen teminatlardan biri veya daha fazlası bulunamadı ya da aktif değil.");
        }

        var results =
            new List<PricingCoverageResult>();

        foreach (var coverage in coverageList)
        {
            decimal calculatedPrice;

            switch (coverage.PricingType)
            {
                case CoveragePricingType.Fixed:

                    calculatedPrice =
                        coverage.BasePrice;

                    break;

                case CoveragePricingType.PercentageOfVehicleValue:

                    if (!coverage.Rate.HasValue)
                    {
                        throw new InvalidOperationException(
                            $"'{coverage.Name}' teminatı için fiyatlandırma oranı tanımlanmamış.");
                    }

                    calculatedPrice =
                        marketValue *
                        coverage.Rate.Value /
                        100m;

                    break;

                default:

                    throw new InvalidOperationException(
                        $"Desteklenmeyen teminat fiyatlandırma tipi: {coverage.PricingType}");
            }

            results.Add(
                new PricingCoverageResult
                {
                    CoverageId =
                        coverage.Id,

                    CoverageName =
                        coverage.Name,

                    CalculatedPrice =
                        decimal.Round(
                            calculatedPrice,
                            2),

                    Limit =
                        coverage.DefaultLimit
                });
        }

        return results;
    }

    private async Task<decimal> GetRuleValueAsync(
        string code,
        DateTime effectiveDate)
    {
        var rule =
            await _unitOfWork
                .PricingRules
                .GetApplicableRuleAsync(
                    code,
                    effectiveDate);

        if (rule == null)
        {
            throw new InvalidOperationException(
                $"Fiyatlandırma kuralı bulunamadı: {code}");
        }

        return rule.Value;
    }

    private async Task<decimal> GetAgeFactorAsync(
        int vehicleAge,
        DateTime effectiveDate)
    {
        var code =
            vehicleAge switch
            {
                <= 2 => "AGE_0_2",
                <= 5 => "AGE_3_5",
                <= 8 => "AGE_6_8",
                <= 12 => "AGE_9_12",
                _ => "AGE_13_15"
            };

        return await GetRuleValueAsync(
            code,
            effectiveDate);
    }

    private async Task<decimal> GetUsageFactorAsync(
        string usage,
        DateTime effectiveDate)
    {
        var normalizedUsage =
            usage?.Trim().ToUpperInvariant();

        var code =
            normalizedUsage switch
            {
                "" or null =>
                    "USAGE_PRIVATE",

                "PRIVATE" =>
                    "USAGE_PRIVATE",

                "COMMERCIAL" =>
                    "USAGE_COMMERCIAL",

                "RENTAL" =>
                    "USAGE_RENTAL",

                _ =>
                    throw new ArgumentException(
                        $"Desteklenmeyen kullanım tipi: {usage}",
                        nameof(usage))
            };

        return await GetRuleValueAsync(
            code,
            effectiveDate);
    }

    private async Task<decimal> GetClaimsFactorAsync(
        int claimsCount,
        DateTime effectiveDate)
    {
        var code =
            claimsCount switch
            {
                <= 0 =>
                    "CLAIMS_0",

                1 =>
                    "CLAIMS_1",

                2 =>
                    "CLAIMS_2",

                _ =>
                    "CLAIMS_3_PLUS"
            };

        return await GetRuleValueAsync(
            code,
            effectiveDate);
    }

    private async Task<decimal> GetDriverFactorAsync(
        int driverAge,
        DateTime effectiveDate)
    {
        var code =
            driverAge switch
            {
                >= 25 or <= 0 =>
                    "DRIVER_25_PLUS",

                >= 21 and <= 24 =>
                    "DRIVER_21_24",

                >= 18 and <= 20 =>
                    "DRIVER_18_20",

                _ =>
                    throw new ArgumentException(
                        $"Desteklenmeyen sürücü yaşı: {driverAge}",
                        nameof(driverAge))
            };

        return await GetRuleValueAsync(
            code,
            effectiveDate);
    }

    private async Task<decimal> GetRegionFactorAsync(
        string region,
        DateTime effectiveDate)
    {
        var normalizedRegion =
            region?.Trim().ToUpperInvariant();

        var code =
            normalizedRegion switch
            {
                "" or null =>
                    "REGION_NORMAL",

                "LOW" =>
                    "REGION_LOW",

                "NORMAL" =>
                    "REGION_NORMAL",

                "HIGH" =>
                    "REGION_HIGH",

                _ =>
                    throw new ArgumentException(
                        $"Desteklenmeyen bölge: {region}",
                        nameof(region))
            };

        return await GetRuleValueAsync(
            code,
            effectiveDate);
    }

    private async Task<decimal> GetPackageFactorAsync(
        Guid? packageId)
    {
        if (!packageId.HasValue)
        {
            return 1.00m;
        }

        var package =
            await _unitOfWork
                .InsurancePackages
                .GetByIdAsync(
                    packageId.Value);

        if (package == null ||
            package.IsDeleted ||
            !package.IsActive)
        {
            throw new InvalidOperationException(
                "Seçilen sigorta paketi bulunamadı veya aktif değil.");
        }

        return package.Factor;
    }

    private Task<decimal> GetDeductibleFactorAsync(
        decimal deductible)
    {
        if (deductible < 0)
        {
            throw new ArgumentException(
                "Muafiyet tutarı negatif olamaz.",
                nameof(deductible));
        }

        return Task.FromResult(1.00m);
    }
}