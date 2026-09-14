namespace Kasko.Business.DTOs.QuickQuote;

public class QuickQuotePolicyCreateRequestDto
{
    public string IdentityNumber { get; set; } = string.Empty;

    public string PhoneNumber { get; set; } = string.Empty;

    public Guid QuoteId { get; set; }

    public Guid VehicleId { get; set; }
}