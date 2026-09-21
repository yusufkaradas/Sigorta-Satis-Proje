using Kasko.DataAccess.Repositories.Abstract;
using Kasko.Entities.Concrete;
using Microsoft.EntityFrameworkCore;

namespace Kasko.DataAccess.Repositories.Concrete
{
    public class PolicyRepository
        : GenericRepository<Policy>, IPolicyRepository
    {
        private readonly KaskoContext _dbContext;

        public PolicyRepository(KaskoContext context)
            : base(context)
        {
            _dbContext = context;
        }

        public async Task<Policy?> GetByIdIncludingDetailsAsync(Guid id)
        {
            return await _dbContext.Policies
                .Include(x => x.Customer)
                .Include(x => x.Vehicle)
                .Include(x => x.Quote)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<bool> PolicyNumberExistsAsync(
            string policyNumber,
            Guid? excludePolicyId = null)
        {
            return await _dbContext.Policies
                .AnyAsync(x =>
                    x.PolicyNumber == policyNumber &&
                    (!excludePolicyId.HasValue ||
                     x.Id != excludePolicyId.Value));
        }

        public async Task<bool> ExistsAsync(Guid id)
        {
            return await _dbContext.Policies
            .AnyAsync(x => x.Id == id);
        }
        public void SetOriginalRowVersion(
    Policy policy,
    byte[] rowVersion)
        {
            _dbContext.Entry(policy)
                .Property(x => x.RowVersion)
                .OriginalValue = rowVersion;
        }
    }
}