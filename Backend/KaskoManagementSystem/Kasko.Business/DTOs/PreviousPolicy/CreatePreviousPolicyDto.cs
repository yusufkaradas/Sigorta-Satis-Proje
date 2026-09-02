namespace Kasko.Business.DTOs.PreviousPolicy;

public class CreatePreviousPolicyDto
{
    public Guid CustomerId { get; set; }

    public string PreviousInsurer { get; set; } = string.Empty;

    public string PolicyNumber { get; set; } = string.Empty;

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public int ClaimsCount { get; set; }
}