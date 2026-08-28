using FluentValidation;
using Kasko.Business.DTOs.Policy;

namespace Kasko.Business.Validators.Policy
{
    public class PolicyUpdateDtoValidator
        : AbstractValidator<PolicyUpdateDto>
    {
        public PolicyUpdateDtoValidator()
        {
            RuleFor(x => x.EndDate)
                .NotEmpty()
                .WithMessage("Poliçe bitiş tarihi zorunludur.");
        }
    }
}