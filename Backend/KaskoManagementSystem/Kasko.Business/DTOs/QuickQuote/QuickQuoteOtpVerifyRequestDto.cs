namespace Kasko.Business.DTOs.QuickQuote;

public class QuickQuoteOtpVerifyRequestDto
{
    public string IdentityNumber { get; set; } = string.Empty;

    public string PhoneNumber { get; set; } = string.Empty;

    public string Code { get; set; } = string.Empty;
}
