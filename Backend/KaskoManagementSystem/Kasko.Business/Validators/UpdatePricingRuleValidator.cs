using FluentValidation;
using Kasko.Business.DTOs.PricingRule;

namespace Kasko.Business.Validators.PricingRule;

public class UpdatePricingRuleValidator
    : AbstractValidator<UpdatePricingRuleDto>
{
    public UpdatePricingRuleValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Value)
            .GreaterThanOrEqualTo(0);
    }
}