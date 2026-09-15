namespace Kasko.Business.DTOs.QuickQuote;

public class QuickQuoteCustomerLookupResponseDto
{
    public bool Found { get; set; }

    public Guid? CustomerId { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;
}