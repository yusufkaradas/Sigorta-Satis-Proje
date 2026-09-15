namespace Kasko.Business.DTOs.QuickQuote;

public class QuickQuoteCustomerLookupRequestDto
{
    public string IdentityNumber { get; set; } = string.Empty;

    public string PhoneNumber { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;
}