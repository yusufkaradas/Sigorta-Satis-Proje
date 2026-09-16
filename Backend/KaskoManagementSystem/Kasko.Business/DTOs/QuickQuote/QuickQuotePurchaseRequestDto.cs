namespace Kasko.Business.DTOs.QuickQuote;

public class QuickQuotePurchaseRequestDto
{
    public Guid QuoteId { get; set; }

    public string IdentityNumber { get; set; } = string.Empty;

    public string PhoneNumber { get; set; } = string.Empty;

    public bool AcceptedTerms { get; set; }

    public bool SimulateFailure { get; set; }
}
