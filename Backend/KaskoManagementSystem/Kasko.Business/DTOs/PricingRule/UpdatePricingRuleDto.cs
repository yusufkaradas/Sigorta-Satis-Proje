namespace Kasko.Business.DTOs.PricingRule;

public class UpdatePricingRuleDto
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public decimal Value { get; set; }

    public bool IsActive { get; set; }
}
