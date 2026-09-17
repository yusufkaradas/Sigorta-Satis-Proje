using FluentValidation;
using Kasko.Business.DTOs.PreviousPolicy;

namespace Kasko.Business.Validators;

public class CreatePreviousPolicyDtoValidator : AbstractValidator<CreatePreviousPolicyDto>
{
    public CreatePreviousPolicyDtoValidator()
    {
        RuleFor(x => x.CustomerId)
            .NotEmpty().WithMessage("Müşteri seçilmelidir.");

        RuleFor(x => x.PreviousInsurer)
            .NotEmpty().WithMessage("Önceki sigorta şirketi boş olamaz.")
            .Length(2, 100).WithMessage("Sigorta şirketi adı 2-100 karakter olmalıdır.");

        RuleFor(x => x.PolicyNumber)
            .NotEmpty().WithMessage("Poliçe numarası boş olamaz.")
            .Length(3, 50).WithMessage("Poliçe numarası 3-50 karakter olmalıdır.")
            .Matches("^[A-Za-z0-9/-]+$").WithMessage("Poliçe numarası sadece harf, rakam, '-' ve '/' içerebilir.");

        RuleFor(x => x.StartDate)
            .GreaterThan(DateTime.UtcNow.AddYears(-10)).WithMessage("Poliçe başlangıcı son 10 yıl içinde olmalıdır.");

        RuleFor(x => x.EndDate)
            .GreaterThan(x => x.StartDate).WithMessage("Bitiş tarihi başlangıç tarihinden sonra olmalıdır.")
            .LessThanOrEqualTo(x => x.StartDate.AddYears(1).AddDays(1)).WithMessage("Poliçe süresi en fazla 1 yıl olabilir.");

        RuleFor(x => x.ClaimsCount)
            .InclusiveBetween(0, 10).WithMessage("Hasar sayısı 0-10 arasında olmalıdır.");
    }
}
