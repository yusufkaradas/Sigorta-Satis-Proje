using Kasko.DataAccess.Repositories.Abstract;
using Kasko.Entities.Concrete;

namespace Kasko.DataAccess.Repositories.Concrete;

public class PricingRuleChangeRequestRepository
    : GenericRepository<PricingRuleChangeRequest>,
      IPricingRuleChangeRequestRepository
{
    public PricingRuleChangeRequestRepository(
        KaskoContext context)
        : base(context)
    {
    }
}