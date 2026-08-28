using FluentValidation;
using Kasko.Business.DTOs.Payment;

namespace Kasko.Business.Validators.Payment
{
    public class PaymentCreateDtoValidator
        : AbstractValidator<PaymentCreateDto>
    {
        public PaymentCreateDtoValidator()
        {
            RuleFor(x => x.PolicyId)
                .NotEmpty()
                .WithMessage("Poliçe bilgisi zorunludur.");
        }
    }
}