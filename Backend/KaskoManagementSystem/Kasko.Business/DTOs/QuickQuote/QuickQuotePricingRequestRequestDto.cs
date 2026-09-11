namespace Kasko.Business.DTOs.QuickQuote;

public class QuickQuotePricingRequestDto
{
    public string IdentityNumber { get; set; } = string.Empty;

    public string PhoneNumber { get; set; } = string.Empty;

    public Guid VehicleId { get; set; }

    public string Usage { get; set; } = "PRIVATE";

    public int ClaimsCount { get; set; }

    public Guid? PackageId { get; set; }

    public decimal Deductible { get; set; }

    public IReadOnlyCollection<Guid> CoverageIds { get; set; }
        = Array.Empty<Guid>();
}