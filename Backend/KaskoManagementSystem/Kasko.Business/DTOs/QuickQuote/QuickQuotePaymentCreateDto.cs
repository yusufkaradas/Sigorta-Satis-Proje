namespace Kasko.Business.DTOs.QuickQuote;

public class QuickQuotePaymentRequestDto
{
    public string IdentityNumber { get; set; } = string.Empty;

    public string PhoneNumber { get; set; } = string.Empty;

    public Guid PolicyId { get; set; }

    public bool SimulateFailure { get; set; }
}