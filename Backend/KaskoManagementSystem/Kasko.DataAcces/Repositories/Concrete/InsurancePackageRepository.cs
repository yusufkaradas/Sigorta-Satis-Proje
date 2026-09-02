using Kasko.DataAccess.Repositories.Abstract;
using Kasko.Entities.Concrete;

namespace Kasko.DataAccess.Repositories.Concrete;

public class InsurancePackageRepository
    : GenericRepository<InsurancePackage>,
      IInsurancePackageRepository
{
    public InsurancePackageRepository(
        KaskoContext context)
        : base(context)
    {
    }
}