namespace Kasko.Business.DTOs.QuickQuote;

public class QuickQuoteEstimateResultDto
{
    public string BrandName { get; set; } = string.Empty;

    public string TypeName { get; set; } = string.Empty;

    public int ModelYear { get; set; }

    public decimal MarketValue { get; set; }

    public List<QuickQuotePackageEstimateDto> Packages { get; set; } = new();
}

public class QuickQuotePackageEstimateDto
{
    public Guid PackageId { get; set; }

    public string PackageCode { get; set; } = string.Empty;

    public string PackageName { get; set; } = string.Empty;

    public string? Description { get; set; }

    public decimal TotalPremium { get; set; }

    public decimal CoveragePremium { get; set; }

    public decimal Discount { get; set; }

    public List<QuickQuoteCoverageEstimateDto> Coverages { get; set; } = new();
}

public class QuickQuoteCoverageEstimateDto
{
    public Guid CoverageId { get; set; }

    public string CoverageName { get; set; } = string.Empty;

    public decimal Price { get; set; }
}
