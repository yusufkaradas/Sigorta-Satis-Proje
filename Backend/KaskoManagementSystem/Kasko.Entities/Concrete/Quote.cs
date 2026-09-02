using Kasko.Entities.Abstract;
using Kasko.Entities.Enums;

namespace Kasko.Entities.Concrete
{
    public class Quote : BaseEntity
    {
        public Guid CustomerId { get; set; }
        public Guid VehicleId { get; set; }

        public String QuoteNumber { get; set; } = String.Empty;
        public decimal PremiumAmount { get; set; }
        public QuoteStatus Status { get; set; }
        public DateTime ValidUntil { get; set; }

        public virtual Customer Customer { get; set; } = null!;
        public virtual Vehicle Vehicle { get; set; } = null!;
        public ICollection<QuoteCoverage> QuoteCoverages { get; set; }
    = new List<QuoteCoverage>();

        public QuotePricingSnapshot? PricingSnapshot { get; set; }
    }
}