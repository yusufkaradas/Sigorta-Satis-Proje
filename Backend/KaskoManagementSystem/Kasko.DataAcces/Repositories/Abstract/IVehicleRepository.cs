using Kasko.DataAccess.Repositories.Abstract;
using Kasko.Entities.Concrete;

namespace Kasko.DataAccess.Repositories
{
    public interface IVehicleRepository : IGenericRepository<Vehicle>
    {
        Task<Vehicle?> GetByIdIncludingDeletedAsync(Guid id);

        Task<bool> ExistsAsync(Guid id);

        Task<bool> PlateExistsAsync(
            string plateNumber,
            Guid? excludeVehicleId = null);

        Task<bool> VinExistsAsync(
            string vin,
            Guid? excludeVehicleId = null);

        Task<bool> DeleteVehicleAsync(
            Guid id,
            Guid? deletedBy);
    }
}