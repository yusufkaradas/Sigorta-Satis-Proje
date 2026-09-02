using Kasko.Entities.Abstract;

namespace Kasko.Entities.Concrete;

public class QuotePricingSnapshot : BaseEntity
{
    public Guid QuoteId { get; set; }

    public decimal MarketValue { get; set; }

    public decimal BaseRate { get; set; }

    public decimal AgeFactor { get; set; }

    public decimal UsageFactor { get; set; }

    public decimal DriverFactor { get; set; }

    public decimal ClaimsFactor { get; set; }

    public decimal RegionFactor { get; set; }

    public decimal PackageFactor { get; set; }

    public decimal DeductibleFactor { get; set; }

    public decimal CoveragePremium { get; set; }

    public decimal Discount { get; set; }

    public decimal FinalPremium { get; set; }
}