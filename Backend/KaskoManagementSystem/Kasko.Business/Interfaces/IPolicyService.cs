using Kasko.Business.DTOs.Policy;

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
    }
}