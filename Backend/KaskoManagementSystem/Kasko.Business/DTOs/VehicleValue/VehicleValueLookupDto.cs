namespace Kasko.Business.DTOs.VehicleValue;

public class VehicleValueLookupDto
{
    public string BrandCode { get; set; } = string.Empty;

    public string TypeCode { get; set; } = string.Empty;

    public string BrandName { get; set; } = string.Empty;

    public string TypeName { get; set; } = string.Empty;

    public int ModelYear { get; set; }

    public decimal Value { get; set; }

    public string Source { get; set; } = string.Empty;

    public DateTime EffectiveDate { get; set; }
}