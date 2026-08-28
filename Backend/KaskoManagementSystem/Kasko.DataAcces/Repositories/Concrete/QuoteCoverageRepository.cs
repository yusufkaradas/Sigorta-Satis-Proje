using Kasko.DataAccess.Repositories.Abstract;
using Kasko.Entities.Concrete;

namespace Kasko.DataAccess.Repositories.Concrete;

public class QuoteCoverageRepository
    : GenericRepository<QuoteCoverage>,
      IQuoteCoverageRepository
{
    public QuoteCoverageRepository(
        KaskoContext context)
        : base(context)
    {
    }
}