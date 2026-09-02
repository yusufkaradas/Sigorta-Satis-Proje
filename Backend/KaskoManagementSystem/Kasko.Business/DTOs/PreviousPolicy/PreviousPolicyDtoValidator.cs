using FluentValidation;

namespace Kasko.Business.DTOs.PreviousPolicy;

public class PreviousPolicyDtoValidator
    : AbstractValidator<CreatePreviousPolicyDto>
{
    public PreviousPolicyDtoValidator()
    {
        RuleFor(x => x.CustomerId)
            .NotEmpty()
            .WithMessage("Müşteri zorunludur.");

        RuleFor(x => x.PreviousInsurer)
            .NotEmpty()
            .MaximumLength(200)
            .WithMessage("Önceki sigorta şirketi zorunludur.");

        RuleFor(x => x.PolicyNumber)
            .NotEmpty()
            .MaximumLength(100)
            .WithMessage("Poliçe numarası zorunludur.");

        RuleFor(x => x.StartDate)
            .NotEmpty()
            .WithMessage("Poliçe başlangıç tarihi zorunludur.");

        RuleFor(x => x.EndDate)
            .NotEmpty()
            .GreaterThanOrEqualTo(x => x.StartDate)
            .WithMessage(
                "Poliçe bitiş tarihi başlangıç tarihinden önce olamaz.");

        RuleFor(x => x.ClaimsCount)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Hasar sayısı negatif olamaz.");
    }
}