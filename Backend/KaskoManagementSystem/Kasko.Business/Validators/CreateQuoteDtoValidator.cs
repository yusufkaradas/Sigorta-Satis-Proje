using FluentValidation;
using Kasko.Business.DTOs.Quote;

namespace Kasko.Business.Validators.Quote
{
    public class CreateQuoteDtoValidator : AbstractValidator<CreateQuoteDto>
    {
        private static readonly string[] AllowedUsages =
            { "PRIVATE", "COMMERCIAL", "RENTAL" };

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

            RuleFor(x => x.Usage)
                .NotEmpty()
                .Must(usage =>
                    AllowedUsages.Contains(
                        usage.Trim().ToUpperInvariant()))
                .WithMessage(
                    "Kullanım tipi PRIVATE, COMMERCIAL veya RENTAL olmalıdır.");

            RuleFor(x => x.ClaimsCount)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Hasar sayısı negatif olamaz.");

            RuleFor(x => x.Deductible)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Muafiyet tutarı negatif olamaz.");
        }
    }
}