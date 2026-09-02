using Kasko.Entities.Abstract;

namespace Kasko.Entities.Concrete;

public class InsurancePackage : BaseEntity
{
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public decimal Factor { get; set; } = 1.00m;

    public bool IsActive { get; set; } = true;

    public ICollection<PackageCoverage> PackageCoverages { get; set; }
        = new List<PackageCoverage>();
}