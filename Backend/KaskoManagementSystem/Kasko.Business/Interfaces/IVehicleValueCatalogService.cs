using Kasko.Business.DTOs.VehicleValue;

namespace Kasko.Business.Interfaces;

public interface IVehicleValueCatalogService
{
    Task<VehicleValueLookupDto?> LookupAsync(
        string brandCode,
        string typeCode,
        int modelYear,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<VehicleValueBrandDto>> GetBrandsAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<VehicleValueTypeDto>> GetTypesAsync(
        string brandCode,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<int>> GetYearsAsync(
    string brandCode,
    string typeCode,
    CancellationToken cancellationToken = default);
}