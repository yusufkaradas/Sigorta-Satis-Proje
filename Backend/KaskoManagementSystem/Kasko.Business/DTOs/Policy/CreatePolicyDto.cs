namespace Kasko.Business.DTOs.Policy
{
    public class PolicyCreateDto
    {
        public Guid CustomerId { get; set; }

        public Guid VehicleId { get; set; }

        public Guid QuoteId { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }
    }
}