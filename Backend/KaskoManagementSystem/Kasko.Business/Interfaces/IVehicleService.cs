using Kasko.Business.DTOs.Vehicle;

namespace Kasko.Business.Services
{
    public interface IVehicleService
    {
        Task<IEnumerable<VehicleListDto>> GetAllAsync(Guid? customerId = null);

        Task<VehicleDto?> GetByIdAsync(Guid id);

        Task<VehicleDto> CreateAsync(CreateVehicleDto dto);

        Task UpdateAsync(Guid id, UpdateVehicleDto dto);

        Task DeleteAsync(Guid id);
    }
}