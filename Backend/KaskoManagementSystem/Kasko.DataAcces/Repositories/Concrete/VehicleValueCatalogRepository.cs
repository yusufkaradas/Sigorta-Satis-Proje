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
}