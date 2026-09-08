using Kasko.Entities.Abstract;

namespace Kasko.Entities.Concrete;

public class VehicleValueCatalog : BaseEntity
{
    public string BrandCode { get; set; } = string.Empty;

    public string TypeCode { get; set; } = string.Empty;

    public string BrandName { get; set; } = string.Empty;

    public string TypeName { get; set; } = string.Empty;

    public string VehicleCategory { get; set; } = string.Empty;

    public int ModelYear { get; set; }

    public decimal Value { get; set; }

    public string Source { get; set; } = "TSB";

    public DateTime EffectiveDate { get; set; }

    public DateTime ImportedAt { get; set; }

    public bool IsActive { get; set; } = true;
}