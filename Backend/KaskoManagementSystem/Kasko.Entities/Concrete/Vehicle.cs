using Kasko.Entities.Abstract;
using Kasko.Entities.Enums;
using Microsoft.VisualBasic.FileIO;

namespace Kasko.Entities.Concrete
{
    public class Vehicle : BaseEntity
    {

        public Guid CustomerId { get; set; }

        public Customer Customer { get; set; } = null!;

        public string PlateNumber { get; set; } = string.Empty;

        public string VIN { get; set; } = string.Empty;

        public string Brand { get; set; } = string.Empty;

        public string? BrandCode { get; set; }

        public string Model { get; set; } = string.Empty;

        public string? TypeCode { get; set; }

        public int ModelYear { get; set; }

        public VehicleType VehicleType { get; set; }

        public FuelType FuelType { get; set; }

        public TransmissionType TransmissionType { get; set; }

        public decimal? EngineVolume { get; set; }

        public int? EnginePower { get; set; }

        public string Color { get; set; } = string.Empty;

        public decimal MarketValue { get; set; }

        public bool IsActive { get; set; }
    }
}