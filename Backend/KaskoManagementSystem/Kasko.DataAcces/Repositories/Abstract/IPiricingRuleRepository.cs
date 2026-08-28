using Kasko.Entities.Concrete;

namespace Kasko.DataAccess.Repositories.Abstract;

public interface IPricingRuleRepository
    : IGenericRepository<PricingRule>
{
    Task<PricingRule?> GetByCodeAsync(
        string code);
}