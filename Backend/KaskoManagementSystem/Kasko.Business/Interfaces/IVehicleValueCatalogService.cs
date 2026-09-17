using Kasko.Business.DTOs.VehicleValue;
using Kasko.Entities.Concrete;

namespace Kasko.Business.Interfaces;

public interface IVehicleValueCatalogService
{
    Task<VehicleValueLookupDto?> LookupAsync(
        string brandCode,
        string typeCode,
        int modelYear,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetCategoriesAsync(
        CancellationToken cancellationToken = default);

    Task<object> GetSummaryAsync();

    Task<int> ReclassifyAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<VehicleValueBrandDto>> GetBrandsAsync(
        string? category,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<VehicleValueBrandDto>> GetBrandsAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<VehicleValueTypeDto>> GetTypesAsync(
        string brandCode,
        string? category,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<VehicleValueTypeDto>> GetTypesAsync(
        string brandCode,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<int>> GetYearsAsync(
    string brandCode,
    string typeCode,
    CancellationToken cancellationToken = default);

    Task<VehicleValueCatalog?> GetActiveByKeyAsync(
    string brandCode,
    string typeCode,
    int modelYear);
    
}