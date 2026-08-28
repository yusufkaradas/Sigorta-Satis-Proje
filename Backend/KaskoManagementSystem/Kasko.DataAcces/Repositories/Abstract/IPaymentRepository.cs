using Kasko.Entities.Concrete;

namespace Kasko.DataAccess.Repositories.Abstract
{
    public interface IPaymentRepository : IGenericRepository<Payment>
    {
        Task<Payment?> GetByIdIncludingDetailsAsync(Guid id);

        Task<bool> TransactionNumberExistsAsync(
            string transactionNumber,
            Guid? excludePaymentId = null);

        Task<bool> HasSuccessfulPaymentAsync(Guid policyId);
    }
}