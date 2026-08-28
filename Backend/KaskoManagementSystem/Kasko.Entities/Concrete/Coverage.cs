using Kasko.Entities.Abstract;
using Kasko.Entities.Enums;

namespace Kasko.Entities.Concrete;

public class Coverage : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public CoveragePricingType PricingType { get; set; }

    public decimal BasePrice { get; set; }

    public decimal? Rate { get; set; }

    public decimal? DefaultLimit { get; set; }

    public bool IsRequired { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<QuoteCoverage> QuoteCoverages { get; set; }
    = new List<QuoteCoverage>();
}