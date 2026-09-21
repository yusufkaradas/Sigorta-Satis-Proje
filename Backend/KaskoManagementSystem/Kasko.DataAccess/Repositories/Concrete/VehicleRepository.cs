using Kasko.DataAccess.Repositories.Concrete;
using Kasko.Entities.Concrete;
using Microsoft.EntityFrameworkCore;

namespace Kasko.DataAccess.Repositories
{
    public class VehicleRepository : GenericRepository<Vehicle>, IVehicleRepository
    {
        private readonly KaskoContext _dbContext;

        public VehicleRepository(KaskoContext context)
            : base(context)
        {
            _dbContext = context;
        }

        public async Task<Vehicle?> GetByIdIncludingDeletedAsync(Guid id)
        {
            return await _dbContext.Vehicles
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<bool> ExistsAsync(Guid id)
        {
            return await _dbContext.Vehicles
                .AnyAsync(x => x.Id == id);
        }

        public async Task<bool> PlateExistsAsync(
            string plateNumber,
            Guid? excludeVehicleId = null)
        {
            return await _dbContext.Vehicles
                .AnyAsync(x =>
                    x.PlateNumber == plateNumber &&
                    (!excludeVehicleId.HasValue ||
                     x.Id != excludeVehicleId.Value));
        }

        public async Task<bool> VinExistsAsync(
            string vin,
            Guid? excludeVehicleId = null)
        {
            return await _dbContext.Vehicles
                .AnyAsync(x =>
                    x.VIN == vin &&
                    (!excludeVehicleId.HasValue ||
                     x.Id != excludeVehicleId.Value));
        }

        public async Task<bool> DeleteVehicleAsync(
            Guid id,
            Guid? deletedBy)
        {
            var vehicle = await _dbContext.Vehicles
                .FirstOrDefaultAsync(
                    x => x.Id == id);

            if (vehicle == null)
            {
                return false;
            }

            vehicle.IsDeleted = true;
            vehicle.DeletedDate = DateTime.UtcNow;
            vehicle.DeletedBy = deletedBy;

            _dbContext.Vehicles.Update(vehicle);

            return true;
        }
    }
}