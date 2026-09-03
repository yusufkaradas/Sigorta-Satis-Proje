using Kasko.Entities.Concrete;

namespace Kasko.DataAccess.Repositories.Abstract;

public interface IVehicleValueCatalogRepository
    : IGenericRepository<VehicleValueCatalog>
{
    Task<IReadOnlyList<VehicleValueCatalog>> GetActiveBrandsAsync();

    Task<IReadOnlyList<VehicleValueCatalog>> GetActiveTypesAsync(
        string brandCode);

    Task<IReadOnlyList<int>> GetActiveYearsAsync(
        string brandCode,
        string typeCode);
    Task<VehicleValueCatalog?> GetActiveByKeyAsync(
    string brandCode,
    string typeCode,
    int modelYear);
}