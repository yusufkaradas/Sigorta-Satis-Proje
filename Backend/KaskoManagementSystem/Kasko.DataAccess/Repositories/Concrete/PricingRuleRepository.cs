using Kasko.DataAccess.Repositories.Abstract;
using Kasko.Entities.Concrete;
using Microsoft.EntityFrameworkCore;

namespace Kasko.DataAccess.Repositories.Concrete;

public class PricingRuleRepository
    : GenericRepository<PricingRule>,
      IPricingRuleRepository
{
    public PricingRuleRepository(
        KaskoContext context)
        : base(context)
    {
    }

    public async Task<PricingRule?> GetByCodeAsync(
        string code)
    {
        return await _context
            .Set<PricingRule>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                !x.IsDeleted &&
                x.IsActive &&
                x.Code == code);
    }
    public async Task<PricingRule?> GetApplicableRuleAsync(
    string code,
    DateTime effectiveDate)
    {
        return await _context
            .Set<PricingRule>()
            .AsNoTracking()
            .Where(x =>
                !x.IsDeleted &&
                x.IsActive &&
                x.Code == code &&
                x.EffectiveFrom <= effectiveDate &&
                (x.EffectiveUntil == null ||
                 x.EffectiveUntil >= effectiveDate))
            .OrderByDescending(x => x.Version)
            .FirstOrDefaultAsync();
    }
}