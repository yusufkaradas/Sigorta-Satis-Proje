namespace Kasko.Business.DTOs.Quote
{
    public class CreateQuoteDto
    {
        public Guid CustomerId { get; set; }
        public Guid VehicleId { get; set; }
        public DateTime ValidUntil { get; set; }

        public DateTime EffectiveDate { get; set; } = DateTime.UtcNow;

        public IReadOnlyCollection<Guid> CoverageIds { get; set; }
            = Array.Empty<Guid>();

        public string Usage { get; set; } = "PRIVATE";

        public int ClaimsCount { get; set; }

        public Guid? PackageId { get; set; }

        public decimal Deductible { get; set; }

        public Guid? PreviousPolicyId { get; set; }
    }
}