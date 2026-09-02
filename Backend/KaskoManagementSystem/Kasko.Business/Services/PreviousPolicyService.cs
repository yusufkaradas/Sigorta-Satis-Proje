using Kasko.DataAccess.Repositories.Abstract;
using Kasko.Entities.Concrete;

namespace Kasko.Business.Services;

public class PreviousPolicyService : IPreviousPolicyService
{
    private readonly IUnitOfWork _unitOfWork;

    public PreviousPolicyService(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PreviousPolicy> CreateAsync(
        PreviousPolicy previousPolicy,
        CancellationToken cancellationToken = default)
    {
        if (previousPolicy.ClaimsCount < 0)
        {
            throw new ArgumentException(
                "Hasar sayısı negatif olamaz.",
                nameof(previousPolicy.ClaimsCount));
        }

        if (previousPolicy.EndDate < previousPolicy.StartDate)
        {
            throw new ArgumentException(
                "Poliçe bitiş tarihi başlangıç tarihinden önce olamaz.");
        }

        await _unitOfWork
            .PreviousPolicies
            .AddAsync(previousPolicy);

        await _unitOfWork.SaveChangesAsync();

        return previousPolicy;
    }

    public async Task<PreviousPolicy?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var previousPolicy =
            await _unitOfWork
                .PreviousPolicies
                .GetByIdAsync(id);

        if (previousPolicy == null ||
            previousPolicy.IsDeleted)
        {
            return null;
        }

        return previousPolicy;
    }

    public async Task<IReadOnlyList<PreviousPolicy>> GetByCustomerIdAsync(
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        var policies =
            await _unitOfWork
                .PreviousPolicies
                .FindAsync(x =>
                    x.CustomerId == customerId &&
                    !x.IsDeleted);

        return policies.ToList();
    }
}