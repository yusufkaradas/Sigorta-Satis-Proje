
using Kasko.Entities.Enums;

namespace Kasko.Business.DTOs.Vehicle
{
    public class CreateVehicleDto
    {
        public Guid CustomerId { get; set; }
        public string PlateNumber { get; set; } = string.Empty;
        public string VIN { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;

        public string BrandCode { get; set; } = string.Empty;

        public string Model { get; set; } = string.Empty;

        public string TypeCode { get; set; } = string.Empty;

        public int ModelYear { get; set; }

        public VehicleType VehicleType { get; set; }

        public FuelType FuelType { get; set; }

        public TransmissionType TransmissionType{ get; set; }

        public decimal? EngineVolume { get; set; }

        public int? EnginePower { get; set; }
        public string Color { get; set; } = string.Empty;

        public decimal MarketValue { get; set; }

    }
}
