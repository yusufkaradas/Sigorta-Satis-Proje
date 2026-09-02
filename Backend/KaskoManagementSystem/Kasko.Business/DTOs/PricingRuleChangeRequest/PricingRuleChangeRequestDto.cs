namespace Kasko.Business.DTOs.PricingRuleChangeRequest;

public class PricingRuleChangeRequestDto
{
    public Guid Id { get; set; }

    public Guid PricingRuleId { get; set; }

    public decimal OldValue { get; set; }

    public decimal NewValue { get; set; }

    public string Reason { get; set; } = string.Empty;

    public Guid RequestedBy { get; set; }

    public DateTime RequestedDate { get; set; }

    public Guid? ApprovedBy { get; set; }

    public DateTime? ApprovedDate { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTime EffectiveFrom { get; set; }
}