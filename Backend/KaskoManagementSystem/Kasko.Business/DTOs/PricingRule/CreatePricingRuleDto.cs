namespace Kasko.Business.DTOs.PricingRule;

public class CreatePricingRuleDto
{
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public decimal Value { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime EffectiveFrom { get; set; }

    public DateTime? EffectiveUntil { get; set; }
}