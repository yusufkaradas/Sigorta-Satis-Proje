using Kasko.Entities.Abstract;

namespace Kasko.Entities.Concrete;

public class PackageCoverage : BaseEntity
{
    public Guid InsurancePackageId { get; set; }

    public Guid CoverageId { get; set; }

    public InsurancePackage InsurancePackage { get; set; } = null!;

    public Coverage Coverage { get; set; } = null!;

    public bool IsDefault { get; set; } = true;
}