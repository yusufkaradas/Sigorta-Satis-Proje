using Kasko.Entities.Concrete;

namespace Kasko.DataAccess.Repositories.Abstract
{
    public interface IPolicyRepository : IGenericRepository<Policy>
    {
        Task<Policy?> GetByIdIncludingDetailsAsync(Guid id);

        Task<bool> PolicyNumberExistsAsync(
            string policyNumber,
            Guid? excludePolicyId = null);

        Task<bool> ExistsAsync(Guid id);

    void SetOriginalRowVersion(
    Policy policy,
    byte[] rowVersion);
    }
}