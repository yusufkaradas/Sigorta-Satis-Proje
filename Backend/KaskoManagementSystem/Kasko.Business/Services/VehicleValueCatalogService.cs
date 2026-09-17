using Kasko.Business.DTOs.VehicleValue;
using Kasko.Business.Interfaces;
using Kasko.DataAccess.Repositories.Abstract;
using Kasko.Entities.Concrete;

namespace Kasko.Business.Services;

public class VehicleValueCatalogService
    : IVehicleValueCatalogService
{
    private readonly IUnitOfWork _unitOfWork;

    public VehicleValueCatalogService(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<VehicleValueCatalog?> GetActiveByKeyAsync(
        string brandCode,
        string typeCode,
        int modelYear)
    {
        return await _unitOfWork
            .VehicleValueCatalogs
            .GetActiveByKeyAsync(
                brandCode,
                typeCode,
                modelYear);
    }

    public async Task<VehicleValueLookupDto?> LookupAsync(
        string brandCode,
        string typeCode,
        int modelYear,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(brandCode))
        {
            throw new ArgumentException(
                "Marka kodu boş olamaz.",
                nameof(brandCode));
        }

        if (string.IsNullOrWhiteSpace(typeCode))
        {
            throw new ArgumentException(
                "Tip kodu boş olamaz.",
                nameof(typeCode));
        }

        var records =
            await _unitOfWork
                .VehicleValueCatalogs
                .FindAsync(x =>
                    !x.IsDeleted &&
                    x.IsActive &&
                    x.BrandCode == brandCode &&
                    x.TypeCode == typeCode &&
                    x.ModelYear == modelYear);

        var record = records
            .OrderByDescending(x => x.EffectiveDate)
            .FirstOrDefault();

        if (record == null)
        {
            return null;
        }

        return new VehicleValueLookupDto
        {
            BrandCode = record.BrandCode,
            TypeCode = record.TypeCode,
            BrandName = record.BrandName,
            TypeName = record.TypeName,
            VehicleCategory = record.VehicleCategory,
            ModelYear = record.ModelYear,
            Value = record.Value,
            Source = record.Source,
            EffectiveDate = record.EffectiveDate
        };
    }

    public async Task<IReadOnlyList<string>> GetCategoriesAsync(
        CancellationToken cancellationToken = default)
    {
        var cached = _categoryCache;

        if (cached != null && cached.Expires > DateTime.UtcNow)
        {
            return cached.Categories;
        }

        var stored =
            await _unitOfWork
                .VehicleValueCatalogs
                .GetActiveCategoriesAsync();

        var categories = VehicleCategoryClassifier.Categories
            .Where(stored.Contains)
            .ToList();

        _categoryCache = new CategoryCache(categories, DateTime.UtcNow.AddMinutes(30));

        return categories;
    }

    private sealed record CategoryCache(IReadOnlyList<string> Categories, DateTime Expires);

    private static CategoryCache? _categoryCache;

    public async Task<int> ReclassifyAsync(
        CancellationToken cancellationToken = default)
    {
        _categoryCache = null;

        return await _unitOfWork
            .VehicleValueCatalogs
            .ReclassifyAsync(VehicleCategoryClassifier.Classify);
    }

    public async Task<IReadOnlyList<VehicleValueBrandDto>> GetBrandsAsync(
        string? category,
        CancellationToken cancellationToken)
    {
        var records =
            await _unitOfWork
                .VehicleValueCatalogs
                .GetActiveBrandsAsync(category);

        return records
            .Select(x => new VehicleValueBrandDto
            {
                Code = x.BrandCode,
                Name = x.BrandName
            })
            .ToList();
    }

    public async Task<IReadOnlyList<VehicleValueBrandDto>> GetBrandsAsync(
        CancellationToken cancellationToken = default)
    {
        var records =
            await _unitOfWork
                .VehicleValueCatalogs
                .GetActiveBrandsAsync();

        return records
            .Select(x => new VehicleValueBrandDto
            {
                Code = x.BrandCode,
                Name = x.BrandName
            })
            .ToList();
    }

    public async Task<IReadOnlyList<VehicleValueTypeDto>> GetTypesAsync(
        string brandCode,
        string? category,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(brandCode))
        {
            throw new ArgumentException(
                "Marka kodu boş olamaz.",
                nameof(brandCode));
        }

        var records =
            await _unitOfWork
                .VehicleValueCatalogs
                .GetActiveTypesAsync(brandCode, category);

        return records
            .Select(x => new VehicleValueTypeDto
            {
                Code = x.TypeCode,
                Name = x.TypeName
            })
            .ToList();
    }

    public async Task<IReadOnlyList<VehicleValueTypeDto>> GetTypesAsync(
        string brandCode,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(brandCode))
        {
            throw new ArgumentException(
                "Marka kodu boş olamaz.",
                nameof(brandCode));
        }

        var records =
            await _unitOfWork
                .VehicleValueCatalogs
                .GetActiveTypesAsync(brandCode);

        return records
            .Select(x => new VehicleValueTypeDto
            {
                Code = x.TypeCode,
                Name = x.TypeName
            })
            .ToList();
    }


    public async Task<IReadOnlyList<int>> GetYearsAsync(
        string brandCode,
        string typeCode,
        CancellationToken cancellationToken = default)
    {
        return await _unitOfWork
            .VehicleValueCatalogs
            .GetActiveYearsAsync(
                brandCode,
                typeCode);
    }
}