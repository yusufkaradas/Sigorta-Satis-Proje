namespace Kasko.Business.DTOs.QuickQuote;

public class QuickQuoteEstimateRequestDto
{
    public string BrandCode { get; set; } = string.Empty;

    public string TypeCode { get; set; } = string.Empty;

    public int ModelYear { get; set; }

    public int BirthYear { get; set; }

    public string Usage { get; set; } = "PRIVATE";

    public int ClaimsCount { get; set; }

    public decimal Deductible { get; set; }

    public Guid? PackageId { get; set; }
}
