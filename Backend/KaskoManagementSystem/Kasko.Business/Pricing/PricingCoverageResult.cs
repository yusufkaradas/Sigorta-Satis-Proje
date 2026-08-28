namespace Kasko.Business.Pricing;

public class PricingCoverageResult
{
    public Guid CoverageId { get; set; }

    public string CoverageName { get; set; } = string.Empty;

    public decimal CalculatedPrice { get; set; }

    public decimal? Limit { get; set; }
}