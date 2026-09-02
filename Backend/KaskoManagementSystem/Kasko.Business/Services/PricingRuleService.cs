using Kasko.Business.DTOs.PricingRule;
using Kasko.Business.Exceptions;
using Kasko.Business.Interfaces;
using Kasko.DataAccess.Repositories.Abstract;
using Kasko.Entities.Concrete;


namespace Kasko.Business.Services;

public class PricingRuleService : IPricingRuleService
{
    private readonly IUnitOfWork _unitOfWork;

    public PricingRuleService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<PricingRuleDto>> GetAllAsync()
    {
        var rules = await _unitOfWork.PricingRules.GetAllAsync();

        return rules
            .Select(MapToDto)
            .ToList();
    }

    public async Task<PricingRuleDto?> GetByIdAsync(Guid id)
    {
        var rule = await _unitOfWork.PricingRules.GetByIdAsync(id);

        if (rule == null || rule.IsDeleted)
        {
            return null;
        }

        return MapToDto(rule);
    }

    public async Task<PricingRuleDto> CreateAsync(
        CreatePricingRuleDto dto)
    {
        var existingRules =
            await _unitOfWork.PricingRules.FindAsync(
                x => x.Code == dto.Code && !x.IsDeleted);

        var nextVersion =
            existingRules.Any()
                ? existingRules.Max(x => x.Version) + 1
                : 1;
        var activePreviousRules = existingRules
    .Where(x =>
        x.IsActive &&
        x.EffectiveUntil == null &&
        x.EffectiveFrom < dto.EffectiveFrom)
    .ToList();

        foreach (var previousRule in activePreviousRules)
        {
            previousRule.EffectiveUntil =
                dto.EffectiveFrom.AddDays(-1);

            await _unitOfWork.PricingRules.UpdateAsync(previousRule);
        }
        var newRule = new PricingRule
        {
            Id = Guid.NewGuid(),
            Code = dto.Code,
            Name = dto.Name,
            Description = dto.Description,
            Value = dto.Value,
            IsActive = dto.IsActive,
            Version = nextVersion,
            EffectiveFrom = dto.EffectiveFrom,
            EffectiveUntil = dto.EffectiveUntil,
            CreatedDate = DateTime.UtcNow
        };

        await _unitOfWork.PricingRules.AddAsync(newRule);
        await _unitOfWork.SaveChangesAsync();

        return MapToDto(newRule);
    }

    public async Task UpdateAsync(
        UpdatePricingRuleDto dto)
    {
        var rule =
            await _unitOfWork.PricingRules.GetByIdAsync(dto.Id);

        if (rule == null || rule.IsDeleted)
        {
            throw new NotFoundException(
                "Pricing rule bulunamadı.");
        }

        rule.Name = dto.Name;
        rule.Description = dto.Description;
        rule.Value = dto.Value;
        rule.IsActive = dto.IsActive;

        await _unitOfWork.PricingRules.UpdateAsync(rule);

        await _unitOfWork.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var rule =
            await _unitOfWork.PricingRules.GetByIdAsync(id);

        if (rule == null || rule.IsDeleted)
        {
            throw new NotFoundException(
                "Pricing rule bulunamadı.");
        }

        await _unitOfWork.PricingRules.DeleteAsync(rule);

        await _unitOfWork.SaveChangesAsync();
    }

    private static PricingRuleDto MapToDto(
        PricingRule rule)
    {
        return new PricingRuleDto
        {
            Id = rule.Id,
            Code = rule.Code,
            Name = rule.Name,
            Description = rule.Description,
            Value = rule.Value,
            IsActive = rule.IsActive,
            Version = rule.Version,
            EffectiveFrom = rule.EffectiveFrom,
            EffectiveUntil = rule.EffectiveUntil,
            CreatedDate = rule.CreatedDate
        };
    }
}