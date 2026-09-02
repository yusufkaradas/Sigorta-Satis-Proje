namespace Kasko.Business.DTOs.Package;

public class InsurancePackageDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Factor { get; set; }
    public bool IsActive { get; set; }

    public List<PackageCoverageDto> Coverages { get; set; } = new();
}