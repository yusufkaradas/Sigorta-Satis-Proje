using Kasko.Entities.Enums;

namespace Kasko.Business.DTOs.PolicyCancellation;

public class CreatePolicyCancellationDto
{
    public Guid PolicyId { get; set; }

    public string Reason { get; set; } = string.Empty;
}

public class DecidePolicyCancellationDto
{
    public string? Note { get; set; }
}

public class PolicyCancellationDto
{
    public Guid Id { get; set; }

    public Guid PolicyId { get; set; }

    public string PolicyNumber { get; set; } = string.Empty;

    public Guid CustomerId { get; set; }

    public string CustomerName { get; set; } = string.Empty;

    public decimal PremiumAmount { get; set; }

    public DateTime PolicyStartDate { get; set; }

    public DateTime PolicyEndDate { get; set; }

    public string Reason { get; set; } = string.Empty;

    public CancellationRequestStatus Status { get; set; }

    public decimal RefundAmount { get; set; }

    public int RemainingDays { get; set; }

    public DateTime RequestedDate { get; set; }

    public DateTime? DecidedDate { get; set; }

    public string? DecisionNote { get; set; }
}
