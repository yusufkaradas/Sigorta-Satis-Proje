using Kasko.Business.DTOs.VehicleValue;
using Kasko.Entities.Enums;

namespace Kasko.Business.DTOs.Quote;

public class QuoteListDto
{
        public Guid? PackageId { get; set; }

        public string? PackageName { get; set; }

        public string? ReviewReason { get; set; }

    public Guid Id { get; set; }

    public Guid CustomerId { get; set; }

    public string CustomerName { get; set; } = string.Empty;

    public Guid VehicleId { get; set; }

    public string VehicleDescription { get; set; } = string.Empty;

    public string PlateNumber { get; set; } = string.Empty;

    public string QuoteNumber { get; set; } = string.Empty;

    public decimal PremiumAmount { get; set; }

    public QuoteStatus Status { get; set; }

    public DateTime ValidUntil { get; set; }

    public DateTime CreatedDate { get; set; }

}