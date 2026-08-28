using Kasko.Entities.Enums;

namespace Kasko.Business.DTOs.Policy
{
    public class PolicyDto
    {
        public Guid Id { get; set; }

        public Guid CustomerId { get; set; }

        public Guid VehicleId { get; set; }

        public Guid QuoteId { get; set; }

        public string PolicyNumber { get; set; } = string.Empty;

        public decimal PremiumAmount { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public DateTime CreatedDate { get; set; }

        public PolicyStatus Status { get; set; }
        public bool IsActive { get; set; }

        public string RowVersion { get; set; } = string.Empty;
    }
}