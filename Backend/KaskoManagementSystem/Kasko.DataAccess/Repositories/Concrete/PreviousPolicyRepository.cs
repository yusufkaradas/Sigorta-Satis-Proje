using Kasko.DataAccess.Repositories.Abstract;
using Kasko.DataAccess.Repositories.Concrete;
using Kasko.Entities.Concrete;

namespace Kasko.DataAccess.Repositories;

public class PreviousPolicyRepository
    : GenericRepository<PreviousPolicy>,
      IPreviousPolicyRepository
{
    public PreviousPolicyRepository(KaskoContext context)
        : base(context)
    {
    }
}