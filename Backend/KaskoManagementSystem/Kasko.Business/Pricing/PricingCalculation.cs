namespace Kasko.Business.Pricing;

public class PricingCalculation
{
    public decimal MarketValue { get; set; }

    public decimal BaseRate { get; set; }

    public decimal AgeFactor { get; set; }

    public decimal BasePremium { get; set; }

    public decimal RiskAdjustedPremium { get; set; }

    public IReadOnlyList<PricingCoverageResult> Coverages { get; set; }
        = Array.Empty<PricingCoverageResult>();

    public decimal CoveragePremium { get; set; }

    public decimal TotalPremium { get; set; }
}