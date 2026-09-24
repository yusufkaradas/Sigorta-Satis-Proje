using Kasko.Entities.Concrete;

namespace Kasko.DataAccess.Repositories.Abstract;

public interface IVehicleValueCatalogRepository
    : IGenericRepository<VehicleValueCatalog>
{
    Task<IReadOnlyList<VehicleValueCatalog>> GetActiveBrandsAsync();

    Task<IReadOnlyList<VehicleValueCatalog>> GetActiveBrandsAsync(string? category);

    Task<IReadOnlyList<string>> GetActiveCategoriesAsync();

    Task<(int Count, int BrandCount, DateTime? LatestEffectiveDate, DateTime? LastImportedAt)> GetSummaryAsync();

    Task<int> ReclassifyAsync(Func<string, string, string> classify);

    Task<IReadOnlyList<VehicleValueCatalog>> GetKeysByEffectiveDateAsync(DateTime effectiveDate);

    Task<IReadOnlyList<VehicleValueCatalog>> GetActiveTypesAsync(
        string brandCode);

    Task<IReadOnlyList<VehicleValueCatalog>> GetActiveTypesAsync(
        string brandCode,
        string? category);

    Task<IReadOnlyList<int>> GetActiveYearsAsync(
        string brandCode,
        string typeCode);

    Task<IReadOnlyList<int>> GetActiveYearsAsync(
        string brandCode,
        string? typeCode,
        string? category);

    Task<IReadOnlyList<VehicleValueCatalog>> GetActiveTypesAsync(
        string brandCode,
        string? category,
        int? modelYear);
    Task<VehicleValueCatalog?> GetActiveByKeyAsync(
    string brandCode,
    string typeCode,
    int modelYear);
}