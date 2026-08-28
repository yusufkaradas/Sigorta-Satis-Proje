using FluentValidation;
using Kasko.Business.DTOs.Policy;

namespace Kasko.Business.Validators.Policy
{
    public class PolicyCreateDtoValidator
        : AbstractValidator<PolicyCreateDto>
    {
        public PolicyCreateDtoValidator()
        {
            RuleFor(x => x.CustomerId)
                .NotEmpty()
                .WithMessage("Müşteri bilgisi zorunludur.");

            RuleFor(x => x.VehicleId)
                .NotEmpty()
                .WithMessage("Araç bilgisi zorunludur.");

            RuleFor(x => x.QuoteId)
                .NotEmpty()
                .WithMessage("Teklif bilgisi zorunludur.");

            RuleFor(x => x.StartDate)
                .NotEmpty()
                .WithMessage("Poliçe başlangıç tarihi zorunludur.");

            RuleFor(x => x.EndDate)
                .NotEmpty()
                .WithMessage("Poliçe bitiş tarihi zorunludur.");

            RuleFor(x => x)
                .Must(x => x.StartDate < x.EndDate)
                .WithMessage(
                    "Poliçe başlangıç tarihi bitiş tarihinden önce olmalıdır.");
        }
    }
}