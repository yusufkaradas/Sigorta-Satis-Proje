using FluentValidation;
using Kasko.Business.DTOs.PricingRule;

namespace Kasko.Business.Validators.PricingRule;

public class CreatePricingRuleValidator
    : AbstractValidator<CreatePricingRuleDto>
{
    public CreatePricingRuleValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Value)
            .GreaterThanOrEqualTo(0);

        RuleFor(x => x.EffectiveFrom)
            .NotEmpty();

        RuleFor(x => x.EffectiveUntil)
            .GreaterThanOrEqualTo(x => x.EffectiveFrom)
            .When(x => x.EffectiveUntil.HasValue);
    }
}