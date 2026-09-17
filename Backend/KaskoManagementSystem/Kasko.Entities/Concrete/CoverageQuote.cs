using Kasko.Entities.Abstract;
using Kasko.Entities.Concrete;

public class QuoteCoverage : BaseEntity
{
    public Guid QuoteId { get; set; }

    public Guid CoverageId { get; set; }

    public decimal CalculatedPrice { get; set; }

    public decimal? Limit { get; set; }

    public Guid? CoverageOptionId { get; set; }

    public string? OptionName { get; set; }

    public Quote Quote { get; set; } = null!;

    public Coverage Coverage { get; set; } = null!;
}