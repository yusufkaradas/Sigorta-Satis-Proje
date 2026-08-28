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
        decimal marketValue,
        int modelYear,
        IReadOnlyCollection<Guid> coverageIds,
        CancellationToken cancellationToken = default)
    {
        if (marketValue <= 0)
        {
            throw new ArgumentException(
                "Araç değeri 0'dan büyük olmalıdır.",
                nameof(marketValue));
        }

        var currentYear =
            DateTime.UtcNow.Year;

        var vehicleAge =
            currentYear - modelYear;

        if (vehicleAge < 0)
        {
            vehicleAge = 0;
        }

        var baseRate =
            await GetRuleValueAsync(
                "BASE_KASKO_RATE");

        var ageFactor =
            await GetAgeFactorAsync(
                vehicleAge);

        var basePremium =
            marketValue * baseRate;

        var riskAdjustedPremium =
            basePremium * ageFactor;

        var coverageResults =
            await CalculateCoveragesAsync(
                marketValue,
                coverageIds);

        var coveragePremium =
            coverageResults.Sum(
                x => x.CalculatedPrice);

        var totalPremium =
            riskAdjustedPremium +
            coveragePremium;

        return new PricingCalculation
        {
            MarketValue = marketValue,

            BaseRate = baseRate,

            AgeFactor = ageFactor,

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
        string code)
    {
        var rule =
            await _unitOfWork
                .PricingRules
                .GetByCodeAsync(code);

        if (rule == null)
        {
            throw new InvalidOperationException(
                $"Fiyatlandırma kuralı bulunamadı: {code}");
        }

        return rule.Value;
    }

    private async Task<decimal> GetAgeFactorAsync(
        int vehicleAge)
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

        return await GetRuleValueAsync(code);
    }
}