namespace Kasko.Business.DTOs.Package;

public class CoverageOptionDto
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public decimal? Limit { get; set; }

    public decimal ExtraPrice { get; set; }

    public bool IsDefault { get; set; }
}
