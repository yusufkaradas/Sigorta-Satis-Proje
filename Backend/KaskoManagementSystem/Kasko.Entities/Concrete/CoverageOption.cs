using Kasko.Entities.Abstract;

namespace Kasko.Entities.Concrete;

public class CoverageOption : BaseEntity
{
    public Guid CoverageId { get; set; }

    public string Name { get; set; } = string.Empty;

    public decimal? Limit { get; set; }

    public decimal ExtraPrice { get; set; }

    public bool IsDefault { get; set; }

    public int SortOrder { get; set; }

    public Coverage Coverage { get; set; } = null!;
}
