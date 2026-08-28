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
            .GroupBy(x => new
            {
                x.BrandCode,
                x.BrandName
            })
            .Select(x => new VehicleValueCatalog
            {
                BrandCode = x.Key.BrandCode,
                BrandName = x.Key.BrandName
            })
            .OrderBy(x => x.BrandName)
            .ToListAsync();
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
            .GroupBy(x => new
            {
                x.TypeCode,
                x.TypeName
            })
            .Select(x => new VehicleValueCatalog
            {
                TypeCode = x.Key.TypeCode,
                TypeName = x.Key.TypeName
            })
            .OrderBy(x => x.TypeName)
            .ToListAsync();
    }
    public async Task<IReadOnlyList<int>> GetActiveYearsAsync(
    string brandCode,
    string typeCode)
    {
        return await _context
            .VehicleValueCatalogs
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
}