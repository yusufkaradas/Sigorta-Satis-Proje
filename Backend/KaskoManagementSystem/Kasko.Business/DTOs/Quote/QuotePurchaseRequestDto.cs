namespace Kasko.Business.DTOs.Quote;

public class QuotePurchaseRequestDto
{
    public bool AcceptedTerms { get; set; }

    public bool SimulateFailure { get; set; }

    public int InstallmentCount { get; set; } = 1;

    public DateTime? StartDate { get; set; }
}
