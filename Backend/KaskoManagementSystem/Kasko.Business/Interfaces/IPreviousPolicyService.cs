using Kasko.Entities.Concrete;

namespace Kasko.Business.Services;

public interface IPreviousPolicyService
{
    Task<PreviousPolicy> CreateAsync(
        PreviousPolicy previousPolicy,
        CancellationToken cancellationToken = default);

    Task<PreviousPolicy?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PreviousPolicy>> GetByCustomerIdAsync(
        Guid customerId,
        CancellationToken cancellationToken = default);
}