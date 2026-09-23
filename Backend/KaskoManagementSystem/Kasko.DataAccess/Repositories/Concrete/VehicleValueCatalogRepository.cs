using Kasko.DataAccess.Repositories.Abstract;
using Kasko.Entities.Concrete;
using Microsoft.EntityFrameworkCore;

namespace Kasko.DataAccess.Repositories.Concrete;

public class VehicleValueCatalogRepository
    : GenericRepository<VehicleValueCatalog>,
      IVehicleValueCatalogRepository
{
    public VehicleValueCatalogRepository(
        KaskoContext context)
        : base(context)
    {
    }

    public async Task<IReadOnlyList<VehicleValueCatalog>> GetActiveBrandsAsync()
    {
        return await _context
            .VehicleValueCatalogs
            .AsNoTracking()
            .Where(x =>
                !x.IsDeleted &&
                x.IsActive)
            .Select(x => new
            {
                x.BrandCode,
                x.BrandName
            })
            .Distinct()
            .OrderBy(x => x.BrandName)
            .Select(x => new VehicleValueCatalog
            {
                BrandCode = x.BrandCode,
                BrandName = x.BrandName
            })
            .ToListAsync();
    }

    public async Task<IReadOnlyList<VehicleValueCatalog>> GetActiveBrandsAsync(
        string? category)
    {
        if (string.IsNullOrWhiteSpace(category))
        {
            return await GetActiveBrandsAsync();
        }

        return await _context
            .VehicleValueCatalogs
            .AsNoTracking()
            .Where(x =>
                !x.IsDeleted &&
                x.IsActive &&
                x.VehicleCategory == category)
            .Select(x => new
            {
                x.BrandCode,
                x.BrandName
            })
            .Distinct()
            .OrderBy(x => x.BrandName)
            .Select(x => new VehicleValueCatalog
            {
                BrandCode = x.BrandCode,
                BrandName = x.BrandName
            })
            .ToListAsync();
    }

    public async Task<IReadOnlyList<string>> GetActiveCategoriesAsync()
    {
        return await _context
            .VehicleValueCatalogs
            .AsNoTracking()
            .Where(x =>
                !x.IsDeleted &&
                x.IsActive &&
                x.VehicleCategory != null &&
                x.VehicleCategory != "")
            .Select(x => x.VehicleCategory)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync();
    }

    public async Task<int> ReclassifyAsync(
        Func<string, string, string> classify)
    {
        var pairs = await _context
            .VehicleValueCatalogs
            .AsNoTracking()
            .Select(x => new { x.BrandName, x.TypeName })
            .Distinct()
            .ToListAsync();

        var updated = 0;

        foreach (var pair in pairs)
        {
            var category =
                classify(pair.BrandName, pair.TypeName);

            updated += await _context
                .VehicleValueCatalogs
                .Where(x =>
                    x.BrandName == pair.BrandName &&
                    x.TypeName == pair.TypeName &&
                    x.VehicleCategory != category)
                .ExecuteUpdateAsync(setters =>
                    setters.SetProperty(x => x.VehicleCategory, category));
        }

        return updated;
    }

    public async Task<IReadOnlyList<VehicleValueCatalog>> GetActiveTypesAsync(
    string brandCode)
    {
        return await _context
            .VehicleValueCatalogs
            .AsNoTracking()
            .Where(x =>
                !x.IsDeleted &&
                x.IsActive &&
                x.BrandCode == brandCode)
            .Select(x => new VehicleValueCatalog
            {
                TypeCode = x.TypeCode,
                TypeName = x.TypeName
            })
            .Distinct()
            .OrderBy(x => x.TypeName)
            .ToListAsync();
    }
    public async Task<IReadOnlyList<VehicleValueCatalog>> GetActiveTypesAsync(
        string brandCode,
        string? category)
    {
        if (string.IsNullOrWhiteSpace(category))
        {
            return await GetActiveTypesAsync(brandCode);
        }

        return await _context
            .VehicleValueCatalogs
            .AsNoTracking()
            .Where(x =>
                !x.IsDeleted &&
                x.IsActive &&
                x.BrandCode == brandCode &&
                x.VehicleCategory == category)
            .Select(x => new VehicleValueCatalog
            {
                TypeCode = x.TypeCode,
                TypeName = x.TypeName
            })
            .Distinct()
            .OrderBy(x => x.TypeName)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<int>> GetActiveYearsAsync(
     string brandCode,
     string typeCode)
    {
        return await _context.VehicleValueCatalogs
            .AsNoTracking()
            .Where(x =>
                !x.IsDeleted &&
                x.IsActive &&
                x.BrandCode == brandCode &&
                x.TypeCode == typeCode)
            .Select(x => x.ModelYear)
            .Distinct()
            .OrderByDescending(x => x)
            .ToListAsync();
    }
    public async Task<IReadOnlyList<int>> GetActiveYearsAsync(
        string brandCode,
        string? typeCode,
        string? category)
    {
        var query = _context.VehicleValueCatalogs
            .AsNoTracking()
            .Where(x =>
                !x.IsDeleted &&
                x.IsActive &&
                x.BrandCode == brandCode);

        if (!string.IsNullOrWhiteSpace(typeCode))
        {
            query = query.Where(x => x.TypeCode == typeCode);
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(x => x.VehicleCategory == category);
        }

        return await query
            .Select(x => x.ModelYear)
            .Distinct()
            .OrderByDescending(x => x)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<VehicleValueCatalog>> GetActiveTypesAsync(
        string brandCode,
        string? category,
        int? modelYear)
    {
        var query = _context.VehicleValueCatalogs
            .AsNoTracking()
            .Where(x =>
                !x.IsDeleted &&
                x.IsActive &&
                x.BrandCode == brandCode);

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(x => x.VehicleCategory == category);
        }

        if (modelYear.HasValue)
        {
            query = query.Where(x => x.ModelYear == modelYear.Value);
        }

        return await query
            .Select(x => new VehicleValueCatalog
            {
                TypeCode = x.TypeCode,
                TypeName = x.TypeName
            })
            .Distinct()
            .OrderBy(x => x.TypeName)
            .ToListAsync();
    }

    public async Task<VehicleValueCatalog?> GetActiveByKeyAsync(
     string brandCode,
     string typeCode,
     int modelYear)
    {
        return await _context.VehicleValueCatalogs
            .AsNoTracking()
            .Where(x =>
                !x.IsDeleted &&
                x.IsActive &&
                x.BrandCode == brandCode &&
                x.TypeCode == typeCode &&
                x.ModelYear == modelYear)
            .OrderByDescending(x => x.EffectiveDate)
            .FirstOrDefaultAsync();
    }

    public async Task<(int Count, int BrandCount, DateTime? LatestEffectiveDate, DateTime? LastImportedAt)> GetSummaryAsync()
    {
        var active = _context.VehicleValueCatalogs
            .AsNoTracking()
            .Where(x => !x.IsDeleted && x.IsActive);

        var count = await active.CountAsync();

        var brandCount = await active.Select(x => x.BrandCode).Distinct().CountAsync();

        var latestEffectiveDate = await active.MaxAsync(x => (DateTime?)x.EffectiveDate);

        var lastImportedAt = await active.MaxAsync(x => (DateTime?)x.ImportedAt);

        return (count, brandCount, latestEffectiveDate, lastImportedAt);
    }
}
