using Kasko.Entities.Concrete;
using Kasko.DataAccess.Repositories.Abstract;

namespace Kasko.DataAccess.Repositories.Concrete;

public class PackageCoverageRepository
    : GenericRepository<PackageCoverage>,
      IPackageCoverageRepository
{
    public PackageCoverageRepository(
        KaskoContext context)
        : base(context)
    {
    }
}