namespace Kasko.Business.DTOs.QuickQuote;

public class QuickQuoteQuotePdfRequestDto
{
    public string IdentityNumber { get; set; } = string.Empty;

    public string PhoneNumber { get; set; } = string.Empty;

    public Guid QuoteId { get; set; }
}
