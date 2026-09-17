using Kasko.Entities.Abstract;
using Kasko.Entities.Enums;

namespace Kasko.Entities.Concrete;

public class PolicyCancellationRequest : BaseEntity
{
    public Guid PolicyId { get; set; }

    public Guid CustomerId { get; set; }

    public string Reason { get; set; } = string.Empty;

    public CancellationRequestStatus Status { get; set; } = CancellationRequestStatus.Pending;

    public decimal RefundAmount { get; set; }

    public int RemainingDays { get; set; }

    public DateTime RequestedDate { get; set; }

    public DateTime? DecidedDate { get; set; }

    public Guid? DecidedBy { get; set; }

    public string? DecisionNote { get; set; }
}
