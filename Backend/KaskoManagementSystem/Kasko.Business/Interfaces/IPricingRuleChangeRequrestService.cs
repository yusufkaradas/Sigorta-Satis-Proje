using Kasko.Business.DTOs.PricingRuleChangeRequest;

namespace Kasko.Business.Interfaces;

public interface IPricingRuleChangeRequestService
{
    Task<PricingRuleChangeRequestDto> CreateAsync(
        CreatePricingRuleChangeRequestDto dto,
        Guid requestedBy);

    Task<IEnumerable<PricingRuleChangeRequestDto>> GetAllAsync();

    Task<PricingRuleChangeRequestDto?> GetByIdAsync(Guid id);

    Task ApproveAsync(Guid id, Guid approvedBy);

    Task RejectAsync(Guid id, Guid approvedBy);
}