using Kasko.Entities.Enums;

namespace Kasko.Business.DTOs.Vehicle
{
    public class VehicleListDto
    {
        public Guid Id { get; set; }

        public Guid CustomerId { get; set; }

        public string PlateNumber { get; set; } = string.Empty;

        public string VIN { get; set; } = string.Empty;

        public string Brand { get; set; } = string.Empty;

        public string Model { get; set; } = string.Empty;

        public int ModelYear { get; set; }

        public VehicleType VehicleType { get; set; }

        public FuelType FuelType { get; set; }

        public DateTime CreatedDate { get; set; }

        public decimal MarketValue { get; set; }

        public bool IsActive { get; set; }
    }
}