using Kasko.Entities.Abstract;
using Kasko.Entities.Enums;

namespace Kasko.Entities.Concrete;

public class TariffChangeRequest : BaseEntity
{
    public string TargetType { get; set; } = string.Empty;

    public Guid TargetId { get; set; }

    public string TargetName { get; set; } = string.Empty;

    public string Field { get; set; } = string.Empty;

    public decimal OldValue { get; set; }

    public decimal NewValue { get; set; }

    public string Reason { get; set; } = string.Empty;

    public TariffRequestStatus Status { get; set; } = TariffRequestStatus.Pending;

    public Guid? RequestedBy { get; set; }

    public string RequestedByName { get; set; } = string.Empty;

    public DateTime RequestedDate { get; set; }

    public Guid? DecidedBy { get; set; }

    public DateTime? DecidedDate { get; set; }

    public string? DecisionNote { get; set; }
}
