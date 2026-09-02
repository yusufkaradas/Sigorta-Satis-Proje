namespace Kasko.Business.DTOs.PricingRule;

public class PricingRuleDto
{
    public Guid Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public decimal Value { get; set; }

    public bool IsActive { get; set; }

    public int Version { get; set; }

    public DateTime EffectiveFrom { get; set; }

    public DateTime? EffectiveUntil { get; set; }

    public DateTime CreatedDate { get; set; }
}