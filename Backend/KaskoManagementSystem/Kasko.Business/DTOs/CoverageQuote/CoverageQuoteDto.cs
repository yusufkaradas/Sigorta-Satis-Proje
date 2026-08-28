public class QuoteCoverageDto
{
    public Guid Id { get; set; }

    public Guid CoverageId { get; set; }

    public string CoverageName { get; set; } = string.Empty;

    public decimal CalculatedPrice { get; set; }

    public decimal? Limit { get; set; }
}