using Kasko.Entities.Enums;

namespace Kasko.Business.DTOs.Quote
{
    public class QuoteDto
    {
        public Guid Id { get; set; }

        public Guid CustomerId { get; set; }

        public string CustomerName { get; set; } = string.Empty;

        public string CustomerEmail { get; set; } = string.Empty;

        public string CustomerPhone { get; set; } = string.Empty;

        public Guid VehicleId { get; set; }

        public string VehicleDescription { get; set; } = string.Empty;

        public string PlateNumber { get; set; } = string.Empty;

        public string Brand { get; set; } = string.Empty;

        public string Model { get; set; } = string.Empty;

        public int ModelYear { get; set; }

        public decimal MarketValue { get; set; }

        public string QuoteNumber { get; set; } = string.Empty;

        public decimal PremiumAmount { get; set; }

        public QuoteStatus Status { get; set; }

        public DateTime ValidUntil { get; set; }

        public DateTime CreatedDate { get; set; }

        public bool IsActive { get; set; }

        public List<QuoteCoverageDto> Coverages { get; set; }
            = new();

        public QuotePricingSnapshotDto? PricingSnapshot { get; set; }
    }


    public class QuoteCoverageDto
    {
        public Guid CoverageId { get; set; }

        public string CoverageName { get; set; } = string.Empty;

        public decimal CalculatedPrice { get; set; }

        public decimal? Limit { get; set; }
    }


    public class QuotePricingSnapshotDto
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

        public decimal CoveragePremium { get; set; }

        public decimal Discount { get; set; }

        public decimal FinalPremium { get; set; }
    }
}