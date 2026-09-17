namespace Kasko.Business.DTOs.Package;

public class PackageCoverageDto
{
    public Guid CoverageId { get; set; }
    public string CoverageName { get; set; } = string.Empty;
    public decimal CalculatedPrice { get; set; }
    public bool IsDefault { get; set; }
    public string? Description { get; set; }
    public List<CoverageOptionDto> Options { get; set; } = new();
}