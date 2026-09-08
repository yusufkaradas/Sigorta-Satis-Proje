using Kasko.Entities.Enums;

namespace Kasko.Business.DTOs.Policy
{
    public class PolicyListDto
    {
        public Guid Id { get; set; }

        public Guid CustomerId { get; set; }

        public Guid VehicleId { get; set; }

        public string CustomerName { get; set; } = string.Empty;

        public string Brand { get; set; } = string.Empty;

        public string Model { get; set; } = string.Empty;

        public string PolicyNumber { get; set; } = string.Empty;

        public decimal PremiumAmount { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public DateTime CreatedDate { get; set; }

        public PolicyStatus Status { get; set; }
    }
}