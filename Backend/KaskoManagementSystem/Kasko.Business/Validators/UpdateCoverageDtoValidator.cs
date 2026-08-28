using FluentValidation;
using Kasko.Business.DTOs.Coverage;
using Kasko.Entities.Enums;

namespace Kasko.Business.Validators.Coverage;

public class UpdateCoverageDtoValidator
    : AbstractValidator<UpdateCoverageDto>
{
    public UpdateCoverageDtoValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Teminat ID zorunludur.");

        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Teminat adı zorunludur.")
            .MaximumLength(100)
            .WithMessage("Teminat adı en fazla 100 karakter olabilir.");

        RuleFor(x => x.Description)
            .MaximumLength(500)
            .WithMessage("Açıklama en fazla 500 karakter olabilir.");

        RuleFor(x => x.PricingType)
            .IsInEnum()
            .WithMessage("Geçersiz fiyatlandırma tipi.");

        When(x => x.PricingType == CoveragePricingType.Fixed, () =>
        {
            RuleFor(x => x.BasePrice)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Sabit fiyat 0 veya daha büyük olmalıdır.");
        });

        When(x => x.PricingType == CoveragePricingType.PercentageOfVehicleValue, () =>
        {
            RuleFor(x => x.Rate)
                .NotNull()
                .WithMessage("Oran bazlı teminatlarda oran zorunludur.")
                .GreaterThan(0)
                .WithMessage("Oran 0'dan büyük olmalıdır.");

            RuleFor(x => x.Rate)
                .LessThanOrEqualTo(100)
                .When(x => x.Rate.HasValue)
                .WithMessage("Oran %100'den büyük olamaz.");
        });

        RuleFor(x => x.DefaultLimit)
            .GreaterThanOrEqualTo(0)
            .When(x => x.DefaultLimit.HasValue)
            .WithMessage("Teminat limiti 0 veya daha büyük olmalıdır.");
    }
}