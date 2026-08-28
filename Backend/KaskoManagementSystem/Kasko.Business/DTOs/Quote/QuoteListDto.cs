using Kasko.Entities.Enums;

namespace Kasko.Business.DTOs.Quote
{
    public class QuoteListDto
    {
        public Guid Id { get; set; }

        public string QuoteNumber { get; set; } = string.Empty;

        public Guid CustomerId { get; set; }

        public Guid VehicleId { get; set; }

        public decimal PremiumAmount { get; set; }

        public QuoteStatus Status { get; set; }

        public DateTime ValidUntil { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}