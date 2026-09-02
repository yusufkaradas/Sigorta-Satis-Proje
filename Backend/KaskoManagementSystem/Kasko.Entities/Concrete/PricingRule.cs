using Kasko.Entities.Abstract;

namespace Kasko.Entities.Concrete;

public class PricingRule : BaseEntity
{
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public decimal Value { get; set; }

    public bool IsActive { get; set; }

    public int Version { get; set; }

    public DateTime EffectiveFrom { get; set; }

    public DateTime? EffectiveUntil { get; set; }
}