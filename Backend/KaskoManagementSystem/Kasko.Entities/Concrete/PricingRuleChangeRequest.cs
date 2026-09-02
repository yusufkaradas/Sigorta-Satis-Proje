using Kasko.Entities.Abstract;

namespace Kasko.Entities.Concrete;

public class PricingRuleChangeRequest : BaseEntity
{
    public Guid PricingRuleId { get; set; }

    public decimal OldValue { get; set; }

    public decimal NewValue { get; set; }

    public string Reason { get; set; } = string.Empty;

    public Guid RequestedBy { get; set; }

    public DateTime RequestedDate { get; set; }

    public Guid? ApprovedBy { get; set; }

    public DateTime? ApprovedDate { get; set; }

    public string Status { get; set; } = "Pending";

    public DateTime EffectiveFrom { get; set; }

    public PricingRule? PricingRule { get; set; }

    public User? Requester { get; set; }

    public User? Approver { get; set; }
}