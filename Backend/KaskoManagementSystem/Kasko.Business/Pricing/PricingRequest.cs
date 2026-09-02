namespace Kasko.Business.Pricing;

public class PricingRequest
{
    public decimal MarketValue { get; set; }

    public int ModelYear { get; set; }

    public int DriverAge { get; set; }

    public string Usage { get; set; } = string.Empty;

    public int ClaimsCount { get; set; }

    public string Region { get; set; } = string.Empty;

    public Guid? PackageId { get; set; }

    public decimal Deductible { get; set; }

    public IReadOnlyCollection<Guid> CoverageIds { get; set; }
        = Array.Empty<Guid>();

    public DateTime EffectiveDate { get; set; }
       = DateTime.UtcNow;
}