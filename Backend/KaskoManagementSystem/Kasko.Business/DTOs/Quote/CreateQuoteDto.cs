namespace Kasko.Business.DTOs.Quote
{
    public class CreateQuoteDto
    {
        public Guid CustomerId { get; set; }
        public Guid VehicleId { get; set; }
        public DateTime ValidUntil { get; set; }

        public IReadOnlyCollection<Guid> CoverageIds { get; set; }
        = Array.Empty<Guid>();
    }
}
