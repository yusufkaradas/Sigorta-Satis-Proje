using Kasko.Entities.Concrete;

namespace Kasko.DataAccess.Repositories.Abstract;

public interface IVehicleValueCatalogRepository
    : IGenericRepository<VehicleValueCatalog>
{
    Task<IReadOnlyList<VehicleValueCatalog>> GetActiveBrandsAsync();

    Task<IReadOnlyList<VehicleValueCatalog>> GetActiveBrandsAsync(string? category);

    Task<IReadOnlyList<string>> GetActiveCategoriesAsync();

    Task<int> ReclassifyAsync(Func<string, string, string> classify);

    Task<IReadOnlyList<VehicleValueCatalog>> GetActiveTypesAsync(
        string brandCode);

    Task<IReadOnlyList<VehicleValueCatalog>> GetActiveTypesAsync(
        string brandCode,
        string? category);

    Task<IReadOnlyList<int>> GetActiveYearsAsync(
        string brandCode,
        string typeCode);
    Task<VehicleValueCatalog?> GetActiveByKeyAsync(
    string brandCode,
    string typeCode,
    int modelYear);
}