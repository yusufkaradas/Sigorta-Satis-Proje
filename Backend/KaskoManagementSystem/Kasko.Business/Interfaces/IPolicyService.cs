using Kasko.Business.DTOs.Policy;
using Kasko.Business.DTOs.Quote;

namespace Kasko.Business.Services.Abstract
{
    public interface IPolicyService
    {
        Task<PolicyDto> CreateAsync(PolicyCreateDto dto);

        Task<PolicyDto?> GetByIdAsync(Guid id);

        Task<IEnumerable<PolicyListDto>> GetAllAsync();

        Task UpdateAsync(Guid id, PolicyUpdateDto dto);

        Task DeleteAsync(Guid id, Guid? deletedBy);

        Task CancelAsync(Guid id, Guid? cancelledBy);

        Task ExpireAsync(Guid id);

        Task<IEnumerable<PolicyListDto>> GetUpcomingRenewalsAsync(
    int daysAhead = 30);
        Task<QuoteDto> RenewAsync(
    PolicyRenewalDto dto);
    }
}