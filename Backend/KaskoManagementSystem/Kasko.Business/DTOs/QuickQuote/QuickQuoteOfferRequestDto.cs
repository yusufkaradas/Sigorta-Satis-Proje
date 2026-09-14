namespace Kasko.Business.DTOs.QuickQuote;

public class QuickQuoteOfferRequestDto
{
    public Guid QuoteId { get; set; }

    public string IdentityNumber { get; set; } = string.Empty;

    public string PhoneNumber { get; set; } = string.Empty;
}