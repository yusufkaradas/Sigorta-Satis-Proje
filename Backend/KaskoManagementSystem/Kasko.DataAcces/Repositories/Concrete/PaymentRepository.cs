using Kasko.DataAccess.Repositories.Abstract;
using Kasko.Entities.Concrete;
using Kasko.Entities.Enums;
using Microsoft.EntityFrameworkCore;

namespace Kasko.DataAccess.Repositories.Concrete
{
    public class PaymentRepository
        : GenericRepository<Payment>, IPaymentRepository
    {
        private readonly KaskoContext _dbContext;

        public PaymentRepository(KaskoContext context)
            : base(context)
        {
            _dbContext = context;
        }

        public async Task<Payment?> GetByIdIncludingDetailsAsync(Guid id)
        {
            return await _dbContext.Payments
                .Include(x => x.Policy)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<bool> TransactionNumberExistsAsync(
            string transactionNumber,
            Guid? excludePaymentId = null)
        {
            return await _dbContext.Payments.AnyAsync(x =>
                                x.TransactionNumber == transactionNumber &&
                                (!excludePaymentId.HasValue ||
                                 x.Id != excludePaymentId.Value));
        }

        public async Task<bool> HasSuccessfulPaymentAsync(Guid policyId)
        {
            return await _dbContext.Payments
                .AnyAsync(x =>
    x.PolicyId == policyId &&
    x.Status == PaymentStatus.Successful);
        }
    }
}