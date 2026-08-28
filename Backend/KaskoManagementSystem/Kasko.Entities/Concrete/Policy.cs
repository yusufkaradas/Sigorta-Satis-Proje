using Kasko.Entities.Abstract;
using Kasko.Entities.Enums;

namespace Kasko.Entities.Concrete
{
    public class Policy : BaseEntity
    {
        public Guid CustomerId { get; set; }
        public Guid VehicleId { get; set; }
        public Guid QuoteId { get; set; }

        public string PolicyNumber { get; set; } = string.Empty;

        public decimal PremiumAmount { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public PolicyStatus Status { get; set; }

        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        public virtual Customer Customer { get; set; } = null!;
        public virtual Vehicle Vehicle { get; set; } = null!;
        public virtual Quote Quote { get; set; } = null!;
    }
}