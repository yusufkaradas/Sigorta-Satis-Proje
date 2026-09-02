namespace Kasko.Business.DTOs.PricingRuleChangeRequest;

public class CreatePricingRuleChangeRequestDto
{
    public Guid PricingRuleId { get; set; }

    public decimal NewValue { get; set; }

    public string Reason { get; set; } = string.Empty;

    public DateTime EffectiveFrom { get; set; }
}