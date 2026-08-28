using FluentValidation;
using Kasko.Business.DTOs.Quote;

namespace Kasko.Business.Validators.Quote
{
    public class CreateQuoteDtoValidator : AbstractValidator<CreateQuoteDto>
    {
        public CreateQuoteDtoValidator()
        {
            RuleFor(x => x.CustomerId)
                .NotEmpty()
                .WithMessage("Müşteri bilgisi zorunludur.");

            RuleFor(x => x.VehicleId)
                .NotEmpty()
                .WithMessage("Araç bilgisi zorunludur.");

            RuleFor(x => x.ValidUntil)
                .Must(validUntil => validUntil > DateTime.UtcNow)
                .WithMessage("Teklif geçerlilik tarihi gelecekte olmalıdır.");
        }
    }
}