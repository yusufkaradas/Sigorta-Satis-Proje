using Kasko.Business.DTOs.PricingRule;

namespace Kasko.Business.Interfaces;

public interface IPricingRuleService
{
    Task<IEnumerable<PricingRuleDto>> GetAllAsync();

    Task<PricingRuleDto?> GetByIdAsync(Guid id);

    Task<PricingRuleDto> CreateAsync(CreatePricingRuleDto dto);

    Task UpdateAsync(UpdatePricingRuleDto dto);

    Task DeleteAsync(Guid id);
}