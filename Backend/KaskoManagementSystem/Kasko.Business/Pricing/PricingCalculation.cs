namespace Kasko.Business.Pricing;

public class PricingCalculation
{
    public decimal MarketValue { get; set; }

    public decimal BaseRate { get; set; }

    public decimal AgeFactor { get; set; }

    public decimal UsageFactor { get; set; }

    public decimal DriverFactor { get; set; }

    public decimal ClaimsFactor { get; set; } 

    public decimal RegionFactor { get; set; }

    public decimal PackageFactor { get; set; }

    public decimal DeductibleFactor { get; set; }

    public decimal BasePremium { get; set; }

    public decimal RiskAdjustedPremium { get; set; }

    public IReadOnlyList<PricingCoverageResult> Coverages { get; set; }
        = Array.Empty<PricingCoverageResult>();

    public decimal CoveragePremium { get; set; }

    public decimal Discount { get; set; }

    public decimal FinalPremium { get; set; }

    public decimal TotalPremium { get; set; }
}