using Kasko.DataAccess.Repositories.Abstract;
using Kasko.Entities.Concrete;

namespace Kasko.DataAccess.Repositories.Concrete;

public class CoverageRepository
    : GenericRepository<Coverage>,
      ICoverageRepository
{
    public CoverageRepository(KaskoContext context)
        : base(context)
    {
    }
}